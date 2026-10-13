using AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;
using AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

namespace AppParque.Api.Modules.Evaluaciones.Application;

/// <summary>
/// Cruza un <see cref="PerfilCapacidades"/> contra los <see cref="RequisitosAtraccion"/> de cada
/// atracción y devuelve un <see cref="DictamenAtraccion"/> con el veredicto y los motivos.
///
/// Reemplaza a <see cref="MotorReglas"/> + <see cref="PreseleccionService"/>, que trabajaban con
/// los 13 grupos. Tres diferencias de fondo:
///
/// 1. <b>No hay grupos.</b> El perfil son dimensiones atómicas, así que una persona ya no tiene que
///    "caber" en una foto predefinida. El caso que lo rompía (amputación transfemoral sin prótesis)
///    aquí se evalúa por lo que es: estado de la pierna + dispositivo + capacidad exigida.
/// 2. <b>El resultado no es un booleano.</b> Hay cuatro veredictos, y el intermedio importa: una
///    atracción puede quedar "permitida con mitigación" o "por verificar en sitio" sin que el
///    sistema tenga que elegir entre un sí falso y un no falso.
/// 3. <b>Sin dato no es "no".</b> <see cref="Respuesta.NoDeterminado"/> produce un motivo de
///    severidad <see cref="Severidad.Verificar"/>, nunca un permiso silencioso.
///
/// El motor es puro: sin estado, sin base de datos, sin I/O. Eso lo hace testeable atracción por
/// atracción, que es como se va a validar con operaciones.
/// </summary>
public static class MotorCapacidades
{
    public static DictamenVisita Evaluar(
        PerfilCapacidades perfil,
        IEnumerable<RequisitosAtraccion> catalogo)
    {
        var dictamenes = catalogo.Select(r => EvaluarAtraccion(perfil, r)).ToList();

        var sinDato = dictamenes
            .SelectMany(d => d.Motivos)
            .Where(m => m.Severidad == Severidad.Verificar && m.Codigo == "dato_faltante")
            .Select(m => m.Dimension)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return new DictamenVisita { Atracciones = dictamenes, DimensionesSinDato = sinDato };
    }

    public static DictamenAtraccion EvaluarAtraccion(PerfilCapacidades perfil, RequisitosAtraccion requisitos)
    {
        var motivos = new List<Motivo>();
        var mitigaciones = new List<string>();

        if (requisitos.SinRequisitosFisicos)
        {
            // H-02: eventos culturales de uso pasivo. No se evalúa nada porque no hay nada que exigir.
            motivos.Add(new Motivo(
                "sin_requisitos", "atraccion",
                "Evento cultural de uso pasivo: no exige capacidades físicas.",
                Severidad.Informativo));

            return Armar(requisitos, perfil.Contexto.Rol, motivos, mitigaciones);
        }

        var rol = ResolverRol(perfil.Contexto.Rol, requisitos);
        var req = requisitos.ParaRol(rol);

        RevisarAntropometria(perfil, req, requisitos, motivos);
        RevisarCondicionesMedicas(perfil, requisitos, motivos);
        RevisarMarcha(perfil, req, motivos);
        RevisarPostura(perfil, req, motivos);
        RevisarManos(perfil, req, motivos);
        RevisarDispositivos(perfil, requisitos.Dispositivos, motivos);
        RevisarSillaRuedas(perfil, requisitos, req, motivos, mitigaciones);
        RevisarSensorialYCognitivo(perfil, requisitos, req, motivos, mitigaciones);
        RevisarEmbarque(perfil, requisitos, req, motivos, mitigaciones);
        RevisarAcompanante(perfil, requisitos, motivos, mitigaciones);
        AgregarAdvertenciasOperativas(requisitos, motivos);

        return Armar(requisitos, rol, motivos, mitigaciones);
    }

    /// <summary>
    /// Si la atracción distingue conductor de pasajero y el visitante no eligió rol, no se puede
    /// dar un veredicto: los requisitos son distintos. Se asume pasajero (el menos exigente) y el
    /// motivo queda marcado para que la app pregunte.
    /// </summary>
    private static RolVisitante ResolverRol(RolVisitante rol, RequisitosAtraccion requisitos)
    {
        if (!requisitos.DistinguePorRol)
            return RolVisitante.Unico;

        return rol == RolVisitante.Unico ? RolVisitante.Pasajero : rol;
    }

    private static void RevisarAntropometria(
        PerfilCapacidades perfil,
        RequisitosRol req,
        RequisitosAtraccion requisitos,
        List<Motivo> motivos)
    {
        var estatura = perfil.Cuerpo.EstaturaCm;

        if (req.AlturaMinimaCm.HasValue || req.AlturaMaximaCm.HasValue)
        {
            if (!estatura.HasValue)
            {
                motivos.Add(Faltante("antropometria.estaturaCm", "la estatura del visitante"));
            }
            else
            {
                if (req.AlturaMinimaCm.HasValue && estatura.Value < req.AlturaMinimaCm.Value)
                    motivos.Add(new Motivo("estatura_minima", "antropometria.estaturaCm",
                        $"Estatura mínima {req.AlturaMinimaCm} cm; el visitante mide {estatura} cm.",
                        Severidad.Bloqueante));

                if (req.AlturaMaximaCm.HasValue && estatura.Value > req.AlturaMaximaCm.Value)
                    motivos.Add(new Motivo("estatura_maxima", "antropometria.estaturaCm",
                        $"Estatura máxima {req.AlturaMaximaCm} cm; el visitante mide {estatura} cm.",
                        Severidad.Bloqueante));
            }
        }

        // H-07: una sola atracción declara peso (Paseo a Caballo, 100 kg), pero el dato se pide en
        // el formulario para descartar aquí y no en la fila.
        if (req.PesoMaximoKg.HasValue)
        {
            if (!perfil.Cuerpo.PesoKg.HasValue)
                motivos.Add(Faltante("antropometria.pesoKg", "el peso del visitante"));
            else if (perfil.Cuerpo.PesoKg.Value > req.PesoMaximoKg.Value)
                motivos.Add(new Motivo("peso_maximo", "antropometria.pesoKg",
                    $"Peso máximo {req.PesoMaximoKg} kg; el visitante pesa {perfil.Cuerpo.PesoKg} kg.",
                    Severidad.Bloqueante));
        }

        if (requisitos.ExigeAcomodacionSeguraVerificada)
        {
            // Única cláusula de las fichas redactada como juicio del operador ("por su tamaño
            // físico no permita ser acomodada en posición segura y la barra cierre correctamente").
            // No se deduce del perfil: se verifica en la atracción.
            var acomodacion = perfil.Postura.AcomodacionSegura;

            if (acomodacion == Respuesta.No)
                motivos.Add(new Motivo("acomodacion_insegura", "postura.acomodacionSegura",
                    "La barra de seguridad no cierra correctamente sobre el visitante.",
                    Severidad.Bloqueante));
            else if (acomodacion == Respuesta.NoDeterminado)
                motivos.Add(new Motivo("acomodacion_por_verificar", "postura.acomodacionSegura",
                    "El operador debe confirmar en la atracción que el visitante queda acomodado " +
                    "en posición segura y que la barra cierra correctamente.",
                    Severidad.Verificar));
        }
    }

    private static void RevisarCondicionesMedicas(
        PerfilCapacidades perfil,
        RequisitosAtraccion requisitos,
        List<Motivo> motivos)
    {
        foreach (var codigo in requisitos.RestriccionesMedicas)
        {
            var valor = perfil.Salud.Obtener(codigo);
            var etiqueta = CodigosCondicion.Etiquetas.TryGetValue(codigo, out var e) ? e : codigo;

            if (valor == Respuesta.Si)
                motivos.Add(new Motivo($"condicion_{codigo}", $"salud.{codigo}",
                    $"La atracción no admite {etiqueta}.", Severidad.Bloqueante));
            else if (valor == Respuesta.NoDeterminado)
                motivos.Add(Faltante($"salud.{codigo}", etiqueta));
        }
    }

    private static void RevisarMarcha(PerfilCapacidades perfil, RequisitosRol req, List<Motivo> motivos)
    {
        if (req.Marcha == "no_requiere")
            return;

        var capacidad = perfil.Marcha.Capacidad;

        if (capacidad == CapacidadMarcha.NoDeterminado)
        {
            motivos.Add(Faltante("marcha.capacidad", "la capacidad de marcha"));
            return;
        }

        if (capacidad == CapacidadMarcha.NoCamina)
        {
            motivos.Add(new Motivo("marcha_no_camina", "marcha.capacidad",
                "La atracción exige desplazarse caminando.", Severidad.Bloqueante));
            return;
        }

        if (req.Marcha == "sin_apoyo" && capacidad == CapacidadMarcha.ConApoyo)
        {
            // H-04: la regla pregunta por la capacidad, nunca por el tipo de ayuda. Da igual que el
            // apoyo sea bastón, muletas o caminador: lo que la ficha exige es caminar sin apoyo.
            motivos.Add(new Motivo("marcha_requiere_sin_apoyo", "marcha.capacidad",
                "La atracción exige caminar sobre los dos pies sin ningún tipo de apoyo.",
                Severidad.Bloqueante));
            return;
        }

        // Las fichas no separan escaleras de marcha: cuando exigen caminar, exigen el mismo nivel
        // de autonomía para subir y bajar. Solo se evalúa si el dato existe, para no endurecer
        // una exigencia que la ficha no escribió.
        var escaleras = perfil.Marcha.Escaleras;

        if (escaleras == CapacidadMarcha.NoCamina)
            motivos.Add(new Motivo("escaleras_no", "marcha.escaleras",
                "La atracción exige subir y bajar escaleras para embarcar.", Severidad.Bloqueante));
        else if (req.Marcha == "sin_apoyo" && escaleras == CapacidadMarcha.ConApoyo)
            motivos.Add(new Motivo("escaleras_requiere_sin_apoyo", "marcha.escaleras",
                "La atracción exige subir y bajar escaleras sin asistencia.", Severidad.Bloqueante));
    }

    private static void RevisarPostura(PerfilCapacidades perfil, RequisitosRol req, List<Motivo> motivos)
    {
        if (req.RequiereSentarseErguido || req.RequiereErguidoTodoElRecorrido)
        {
            var sedestacion = perfil.Postura.SedestacionErguida;

            if (sedestacion == SedestacionErguida.NoDeterminado)
            {
                motivos.Add(Faltante("postura.sedestacionErguida", "la capacidad de sentarse erguido"));
            }
            else if (sedestacion == SedestacionErguida.No)
            {
                motivos.Add(new Motivo("sedestacion_no", "postura.sedestacionErguida",
                    "La atracción exige mantenerse sentado erguido.", Severidad.Bloqueante));
            }
            else if (sedestacion == SedestacionErguida.ConApoyo)
            {
                motivos.Add(new Motivo("sedestacion_con_apoyo", "postura.sedestacionErguida",
                    "La atracción exige mantenerse sentado erguido por sí solo, sin apoyo.",
                    Severidad.Bloqueante));
            }
            else if (req.RequiereErguidoTodoElRecorrido && sedestacion == SedestacionErguida.Autonoma)
            {
                // Los dos carruseles piden la variante estricta, que es un valor distinto y no un
                // matiz del mismo: "mantenerse sentado erguido por sí solo durante todo el recorrido".
                motivos.Add(new Motivo("sedestacion_todo_recorrido", "postura.sedestacionErguida",
                    "La atracción exige mantenerse erguido por sí solo durante todo el recorrido.",
                    Severidad.Verificar));
            }
        }

        if (!req.RequiereDoblarPiernas)
            return;

        if (perfil.Postura.FlexionPiernas == Respuesta.No)
            motivos.Add(new Motivo("flexion_piernas_no", "postura.flexionPiernas",
                "La atracción exige doblar las piernas para sentarse en posición segura.",
                Severidad.Bloqueante));
        else if (perfil.Postura.FlexionPiernas == Respuesta.NoDeterminado)
            motivos.Add(Faltante("postura.flexionPiernas", "la capacidad de doblar las piernas"));
    }

    private static void RevisarManos(PerfilCapacidades perfil, RequisitosRol req, List<Motivo> motivos)
    {
        var requeridas = req.Agarre switch
        {
            "ambas_manos" => 2,
            "una_mano" => 1,
            _ => 0,
        };

        if (requeridas == 0 && !req.RequiereManiobrarDosManos)
            return;

        if (perfil.Manos.Ambas.Any(m => m.Agarre == CapacidadAgarre.NoDeterminado))
        {
            motivos.Add(Faltante("mano.agarre", "la capacidad de agarre de las manos"));
            return;
        }

        var funcionales = perfil.Manos.Cuenta(m => m.Agarre == CapacidadAgarre.Funcional);

        if (funcionales < requeridas)
            motivos.Add(new Motivo("agarre_insuficiente", "mano.agarre",
                requeridas == 2
                    ? "La atracción exige agarre funcional con ambas manos."
                    : "La atracción exige agarre funcional con al menos una mano.",
                Severidad.Bloqueante));

        if (!req.RequiereManiobrarDosManos)
            return;

        // H-06: maniobrar es una capacidad distinta de sujetarse, y solo la exigen Paseo a Caballo y
        // los Karts — donde el visitante queda expuesto y conduce. En botes y carros chocones el
        // ambiente es controlado y la ficha pide únicamente sujetarse.
        if (funcionales < 2)
        {
            motivos.Add(new Motivo("maniobra_insuficiente", "manos.maniobra",
                "La atracción exige sujetar y maniobrar con ambas manos.", Severidad.Bloqueante));
            return;
        }

        if (perfil.Manos.Ambas.Any(m => m.Estado == EstadoMano.NoDeterminado))
            motivos.Add(Faltante("mano.estado", "el estado de las manos"));
        else if (perfil.Manos.Ambas.Any(m => m.Estado != EstadoMano.Integra) ||
                 perfil.Manos.Ambas.Any(m => m.Acortamiento == Respuesta.Si))
            motivos.Add(new Motivo("maniobra_manos_no_integras", "manos.maniobra",
                "La atracción exige maniobrar con ambas manos íntegras y sin acortamiento.",
                Severidad.Bloqueante));
    }

    /// <summary>
    /// El eje del aparato, separado del estado del cuerpo. Es lo que resuelve H-08: Rin-Rin prohíbe
    /// la prótesis sobre rodilla y no exige rodilla completa, así que una amputación transfemoral
    /// SIN prótesis no choca con nada — la ficha restringe el dispositivo, no el muñón.
    /// </summary>
    private static void RevisarDispositivos(
        PerfilCapacidades perfil,
        RequisitosDispositivos dispositivos,
        List<Motivo> motivos)
    {
        var protesisPierna = perfil.Piernas.Ambas.Where(p => p.Protesis is not null).Select(p => p.Protesis!).ToList();
        var protesisMano = perfil.Manos.Ambas.Where(m => m.Protesis is not null).Select(m => m.Protesis!).ToList();
        var usaAlgunaProtesis = protesisPierna.Count > 0 || protesisMano.Count > 0;

        if (!dispositivos.PermiteCualquierProtesis && usaAlgunaProtesis)
        {
            motivos.Add(new Motivo("protesis_ninguna_permitida", "dispositivos.protesis",
                "La atracción no permite ningún tipo de prótesis de extremidades.",
                Severidad.Bloqueante));
        }
        else
        {
            if (!dispositivos.PermiteProtesisMano && protesisMano.Count > 0)
                motivos.Add(new Motivo("protesis_mano_no_permitida", "mano.protesis",
                    "La atracción no permite prótesis de mano.", Severidad.Bloqueante));

            if (!dispositivos.PermiteProtesisPiernaSobreRodilla &&
                protesisPierna.Any(p => p.Nivel == NivelProtesis.SobreRodilla))
                motivos.Add(new Motivo("protesis_pierna_sobre_rodilla_no_permitida", "pierna.protesis",
                    "La atracción no permite prótesis de pierna por encima de la rodilla.",
                    Severidad.Bloqueante));

            if (!dispositivos.PermiteProtesisPiernaBajoRodilla &&
                protesisPierna.Any(p => p.Nivel == NivelProtesis.BajoRodilla))
                motivos.Add(new Motivo("protesis_pierna_bajo_rodilla_no_permitida", "pierna.protesis",
                    "La atracción no permite prótesis de pierna por debajo de la rodilla.",
                    Severidad.Bloqueante));
        }

        // H-03, pendiente de reunión con las partes interesadas: el motor NO evalúa todavía la
        // configuración "sin la prótesis". Solo deja constancia de que el veredicto podría cambiar,
        // para que el personal lo consulte y para que la decisión se tome con el caso delante.
        var bloqueadoPorProtesis = motivos.Any(m =>
            m.Severidad == Severidad.Bloqueante && m.Codigo.StartsWith("protesis_"));

        if (bloqueadoPorProtesis &&
            protesisPierna.Concat(protesisMano).Any(p => p.Removible == Respuesta.Si))
        {
            motivos.Add(new Motivo("protesis_removible_h03", "dispositivos.protesisRemovible",
                "El visitante declara que la prótesis es removible. El resultado sin la prótesis " +
                "no se calcula todavía: el uso de atracciones sin la prótesis está pendiente de " +
                "aprobación (H-03).",
                Severidad.Informativo));
        }

        if (dispositivos.ExigeAsegurarProtesis && usaAlgunaProtesis)
            motivos.Add(new Motivo("asegurar_protesis", "dispositivos.protesis",
                "Debe asegurar bien prótesis y órtesis de extremidades antes de embarcar.",
                Severidad.Informativo));
    }

    private static void RevisarSillaRuedas(
        PerfilCapacidades perfil,
        RequisitosAtraccion requisitos,
        RequisitosRol req,
        List<Motivo> motivos,
        List<string> mitigaciones)
    {
        var uso = perfil.Embarque.SillaRuedas;

        if (uso == UsoSillaRuedas.NoAplica)
            return;

        var silla = requisitos.SillaRuedas;

        if (!silla.Admitida)
        {
            // La ficha no modela la silla: el visitante tiene que salir de ella por sus propios
            // medios y cumplir el resto de los requisitos como cualquier otro.
            if (uso is UsoSillaRuedas.PermanenciaEstable or UsoSillaRuedas.NoSeTransfiere)
                motivos.Add(new Motivo("silla_no_admitida", "embarque.sillaRuedas",
                    "La atracción no cuenta con cabina accesible: el visitante debe salir de la " +
                    "silla de ruedas y acomodarse en el asiento.", Severidad.Bloqueante));

            return;
        }

        if (uso == UsoSillaRuedas.NoSeTransfiere && !silla.AdmitePermanencia)
        {
            motivos.Add(new Motivo("silla_requiere_transferencia", "embarque.sillaRuedas",
                "La atracción exige transferirse de la silla de ruedas a la cabina.",
                Severidad.Bloqueante));
            return;
        }

        if (uso == UsoSillaRuedas.PermanenciaEstable && !silla.AdmitePermanencia)
        {
            motivos.Add(new Motivo("silla_sin_permanencia", "embarque.sillaRuedas",
                "La atracción no permite permanecer en la silla de ruedas: exige transferencia a la cabina.",
                Severidad.Bloqueante));
            return;
        }

        if (uso == UsoSillaRuedas.TransferenciaConAyuda && silla.AdmiteTransferencia && !silla.AdmitePermanencia)
        {
            var puedeAsistir = requisitos.Acompanante.Mitigaciones
                .Contains(CodigosMitigacion.AsistenciaEmbarqueAcompanante);

            if (puedeAsistir)
            {
                motivos.Add(new Motivo("silla_transferencia_asistida", "embarque.sillaRuedas",
                    "La transferencia a la cabina debe hacerla el acompañante adulto.",
                    Severidad.Condicion));
                mitigaciones.Add(CodigosMitigacion.AsistenciaEmbarqueAcompanante);
            }
            else if (req.RequiereEmbarqueAutonomo)
            {
                motivos.Add(new Motivo("silla_transferencia_no_autonoma", "embarque.sillaRuedas",
                    "La atracción exige transferirse de la silla de ruedas por sí solo.",
                    Severidad.Bloqueante));
            }
        }
    }

    private static void RevisarSensorialYCognitivo(
        PerfilCapacidades perfil,
        RequisitosAtraccion requisitos,
        RequisitosRol req,
        List<Motivo> motivos,
        List<string> mitigaciones)
    {
        var s = requisitos.Sensoriales;
        var sensorial = perfil.Sensorial;

        if (sensorial.Vision == NivelVision.Total && !s.PermiteVisualTotal)
            motivos.Add(new Motivo("vision_total", "sensorial.vision",
                "La atracción no admite discapacidad visual total.", Severidad.Bloqueante));
        else if (sensorial.Vision == NivelVision.Total && s.VisualTotalRequiereAcompanante)
            AgregarCondicionAcompanante(motivos, mitigaciones, "vision_total_con_acompanante",
                "sensorial.vision", "Con discapacidad visual total debe ir con acompañante adulto.");

        if (sensorial.Vision == NivelVision.Parcial && !s.PermiteVisualParcial)
            motivos.Add(new Motivo("vision_parcial", "sensorial.vision",
                "La atracción no admite discapacidad visual parcial.", Severidad.Bloqueante));

        if (sensorial.Audicion is NivelAudicion.Total or NivelAudicion.Parcial && !s.PermiteAuditiva)
            motivos.Add(new Motivo("audicion", "sensorial.audicion",
                "La atracción no admite discapacidad auditiva: el visitante debe oír las " +
                "indicaciones durante el recorrido.", Severidad.Bloqueante));

        if (sensorial.Cognitivo == NivelCognitivo.SeveraCertificada && !s.PermiteCognitivaSeveraCertificada)
            motivos.Add(new Motivo("cognitiva_severa", "sensorial.cognitivo",
                "La atracción no admite discapacidad cognitiva severa certificada.", Severidad.Bloqueante));
        else if (sensorial.Cognitivo == NivelCognitivo.Leve && !s.PermiteCognitivaLeve)
            motivos.Add(new Motivo("cognitiva_leve", "sensorial.cognitivo",
                "La atracción no admite discapacidad cognitiva.", Severidad.Bloqueante));
        else if (sensorial.Cognitivo == NivelCognitivo.Leve && s.CognitivaLeveRequiereAcompanante)
            AgregarCondicionAcompanante(motivos, mitigaciones, "cognitiva_leve_con_acompanante",
                "sensorial.cognitivo", "Con discapacidad cognitiva leve debe ir con acompañante adulto.");

        if (!req.RequiereComprensionInstrucciones)
            return;

        // H-05: dimensión propia, no un derivado de "discapacidad cognitiva". Toda atracción con
        // barra de seguridad depende de que la persona se mantenga sentada y no se suelte, y eso es
        // comprender la instrucción — no un diagnóstico.
        if (sensorial.ComprendeInstrucciones == Respuesta.No)
            motivos.Add(new Motivo("no_comprende_instrucciones", "sensorial.comprendeInstrucciones",
                "La atracción exige comprender y seguir las instrucciones de seguridad durante " +
                "todo el recorrido.", Severidad.Bloqueante));
        else if (sensorial.ComprendeInstrucciones == Respuesta.NoDeterminado)
            motivos.Add(Faltante("sensorial.comprendeInstrucciones",
                "la comprensión de las instrucciones de seguridad"));
    }

    private static void RevisarEmbarque(
        PerfilCapacidades perfil,
        RequisitosAtraccion requisitos,
        RequisitosRol req,
        List<Motivo> motivos,
        List<string> mitigaciones)
    {
        if (!req.RequiereEmbarqueAutonomo || perfil.Embarque.SillaRuedas != UsoSillaRuedas.NoAplica)
            return;

        if (perfil.Embarque.Autonomo == Respuesta.Si)
            return;

        var puedeAsistir = requisitos.Acompanante.Mitigaciones
            .Contains(CodigosMitigacion.AsistenciaEmbarqueAcompanante);

        if (perfil.Embarque.Autonomo == Respuesta.NoDeterminado)
        {
            motivos.Add(Faltante("embarque.autonomo", "la autonomía para embarcar y desembarcar"));
            return;
        }

        if (puedeAsistir)
        {
            motivos.Add(new Motivo("embarque_asistido", "embarque.autonomo",
                "Si requiere asistencia para embarcar o desembarcar, debe hacerlo el acompañante adulto.",
                Severidad.Condicion));
            mitigaciones.Add(CodigosMitigacion.AsistenciaEmbarqueAcompanante);
        }
        else
        {
            motivos.Add(new Motivo("embarque_no_autonomo", "embarque.autonomo",
                "La atracción exige entrar, embarcar, desembarcar y salir por sí solo.",
                Severidad.Bloqueante));
        }
    }

    /// <summary>
    /// H-11: el umbral niño/adulto es una ESTATURA y es distinta en cada atracción (110, 120, 130,
    /// 140 o 150 cm). H-10: el acompañante válido es un adulto o una persona por encima de la
    /// estatura mínima que fije la ficha, y tiene restricciones propias (los carruseles exigen que
    /// no esté embarazado).
    /// </summary>
    private static void RevisarAcompanante(
        PerfilCapacidades perfil,
        RequisitosAtraccion requisitos,
        List<Motivo> motivos,
        List<string> mitigaciones)
    {
        var reglas = requisitos.Acompanante;
        var acompanante = perfil.Contexto.Acompanante;

        var requerido = false;

        if (reglas.UmbralEstaturaCm.HasValue && perfil.Cuerpo.EstaturaCm.HasValue &&
            perfil.Cuerpo.EstaturaCm.Value < reglas.UmbralEstaturaCm.Value)
        {
            requerido = true;
            motivos.Add(new Motivo("acompanante_por_estatura", "contexto.acompanante",
                $"Por debajo de {reglas.UmbralEstaturaCm} cm debe ir con acompañante adulto" +
                (reglas.EstaturaMinimaAcompananteCm.HasValue
                    ? $" o con una persona de más de {reglas.EstaturaMinimaAcompananteCm} cm."
                    : "."),
                Severidad.Condicion));
        }

        if (reglas.RequeridoPorCognitiva && perfil.Sensorial.Cognitivo is NivelCognitivo.Leve or NivelCognitivo.SeveraCertificada)
        {
            requerido = true;
            motivos.Add(new Motivo("acompanante_por_cognitiva", "contexto.acompanante",
                "Con discapacidad cognitiva debe ir con acompañante adulto.", Severidad.Condicion));
        }

        if (reglas.RequeridoPorVisual && perfil.Sensorial.Vision == NivelVision.Total)
        {
            requerido = true;
            motivos.Add(new Motivo("acompanante_por_visual", "contexto.acompanante",
                "Con discapacidad visual total debe ir con acompañante adulto.", Severidad.Condicion));
        }

        if (reglas.RequeridoPorMovilidadReducida && perfil.Marcha.Capacidad == CapacidadMarcha.ConApoyo)
        {
            requerido = true;
            motivos.Add(new Motivo("acompanante_por_movilidad", "contexto.acompanante",
                "Con movilidad reducida debe ir con acompañante adulto.", Severidad.Condicion));
        }

        if (!requerido && mitigaciones.Count == 0)
            return;

        foreach (var mitigacion in reglas.Mitigaciones.Where(m => m != CodigosMitigacion.AsistenciaEmbarqueAcompanante))
            mitigaciones.Add(mitigacion);

        if (requerido)
            mitigaciones.Add(CodigosMitigacion.AcompananteAdulto);

        // "No se preguntó" y "respondió que viene solo" no son lo mismo, y confundirlos hacía que el
        // sistema pidiera verificar en sitio algo que el enfermero ya había contestado.
        if (acompanante is null)
        {
            motivos.Add(new Motivo("acompanante_sin_dato", "contexto.acompanante",
                "No se registró si viene con acompañante: el personal debe confirmarlo en el ingreso.",
                Severidad.Verificar));
            return;
        }

        if (!acompanante.Presente)
        {
            motivos.Add(new Motivo("acompanante_ausente", "contexto.acompanante",
                "La atracción exige acompañante y el visitante viene solo.",
                Severidad.Bloqueante));
            return;
        }

        var estaturaSuficiente = reglas.EstaturaMinimaAcompananteCm.HasValue &&
                                 acompanante.EstaturaCm.HasValue &&
                                 acompanante.EstaturaCm.Value > reglas.EstaturaMinimaAcompananteCm.Value;

        if (!acompanante.EsAdulto && !estaturaSuficiente)
            motivos.Add(new Motivo("acompanante_no_apto", "contexto.acompanante",
                reglas.EstaturaMinimaAcompananteCm.HasValue
                    ? $"El acompañante debe ser adulto o medir más de {reglas.EstaturaMinimaAcompananteCm} cm."
                    : "El acompañante debe ser adulto.",
                Severidad.Bloqueante));

        // El acompañante es un segundo perfil con requisitos propios, no una casilla.
        foreach (var codigo in requisitos.RestriccionesMedicasAcompanante)
        {
            var etiqueta = CodigosCondicion.Etiquetas.TryGetValue(codigo, out var e) ? e : codigo;

            var valor = codigo == CodigosCondicion.Embarazo
                ? acompanante.Embarazo
                : Respuesta.NoDeterminado;

            if (valor == Respuesta.Si)
                motivos.Add(new Motivo($"acompanante_condicion_{codigo}", "contexto.acompanante",
                    $"El acompañante no debe presentar {etiqueta}.", Severidad.Bloqueante));
            else if (valor == Respuesta.NoDeterminado)
                motivos.Add(new Motivo("acompanante_condicion_por_verificar", "contexto.acompanante",
                    $"Verificar con el acompañante: {etiqueta}.", Severidad.Verificar));
        }
    }

    private static void AgregarAdvertenciasOperativas(RequisitosAtraccion requisitos, List<Motivo> motivos)
    {
        foreach (var mitigacion in requisitos.Acompanante.Mitigaciones)
        {
            if (mitigacion == CodigosMitigacion.AsistenciaEmbarqueAcompanante)
                continue;

            if (CodigosMitigacion.Etiquetas.TryGetValue(mitigacion, out var etiqueta) &&
                motivos.All(m => m.Codigo != $"advertencia_{mitigacion}"))
                motivos.Add(new Motivo($"advertencia_{mitigacion}", "atraccion.advertencia",
                    char.ToUpperInvariant(etiqueta[0]) + etiqueta[1..] + ".", Severidad.Informativo));
        }
    }

    private static void AgregarCondicionAcompanante(
        List<Motivo> motivos,
        List<string> mitigaciones,
        string codigo,
        string dimension,
        string texto)
    {
        motivos.Add(new Motivo(codigo, dimension, texto, Severidad.Condicion));
        mitigaciones.Add(CodigosMitigacion.AcompananteAdulto);
    }

    private static Motivo Faltante(string dimension, string queFalta) => new(
        "dato_faltante", dimension,
        $"Falta el dato: {queFalta}. Sin ese dato la atracción no se puede permitir ni descartar.",
        Severidad.Verificar);

    private static DictamenAtraccion Armar(
        RequisitosAtraccion requisitos,
        RolVisitante rol,
        List<Motivo> motivos,
        List<string> mitigaciones)
    {
        // El veredicto es la severidad más grave que se encontró. Es deliberado que "falta un dato"
        // no permita y tampoco restrinja: el modelo anterior lo resolvía como permiso silencioso.
        var veredicto = Veredicto.Permitida;

        if (motivos.Any(m => m.Severidad == Severidad.Bloqueante))
            veredicto = Veredicto.Restringida;
        else if (motivos.Any(m => m.Severidad == Severidad.Verificar))
            veredicto = Veredicto.RequiereVerificacionPresencial;
        else if (motivos.Any(m => m.Severidad == Severidad.Condicion))
            veredicto = Veredicto.PermitidaConMitigacion;

        return new DictamenAtraccion
        {
            CodigoAtraccion = requisitos.Codigo,
            NombreAtraccion = requisitos.Nombre,
            Rol = rol,
            Veredicto = veredicto,
            Motivos = motivos,
            MitigacionesRequeridas = veredicto == Veredicto.Restringida
                ? []
                : mitigaciones.Distinct().ToList(),
        };
    }
}
