using System.Text.Json;
using System.Text.Json.Serialization;
using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using AppParque.Api.Modules.CatalogoPreguntas.Domain;
using AppParque.Api.Modules.CatalogoZonas.Domain;
using AppParque.Api.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Data;

/// <summary>
/// Carga inicial de catálogos, usuarios y atracciones a partir de Data/Seed/admin.json
/// (copia de AppParque/DB_data/admin.JSON, la data real usada por el prototipo).
/// Idempotente: no hace nada si ya hay usuarios en la base de datos.
/// </summary>
public static class DbSeeder
{
    // Grupo/Condicion no vienen con nombre en el admin.json (solo el código como clave de un bool),
    // así que el catálogo con nombre/descripción se define aquí a partir de la sección 5 del
    // documento de requisitos. Pendiente de confirmar con el conocimiento del negocio (ver
    // Requierements/Requisitos-App-Evaluacion-Accesibilidad.md, sección 9).
    private static readonly (string Codigo, string Nombre, string? Descripcion)[] GruposCatalogo =
    [
        ("grupo_1", "Grupo 1", "No puede caminar sin apoyo"),
        ("grupo_1_1", "Grupo 1.1", "Camina sin apoyo, sube escaleras sin ayuda, sin capacidad de agarre"),
        ("grupo_2", "Grupo 2", "Camina sin apoyo, agarre en una sola mano"),
        ("grupo_3", "Grupo 3", "Sube escaleras con ayuda, agarre en una sola mano"),
        ("grupo_4", "Grupo 4", "Sube escaleras con ayuda, agarre en ambas manos"),
        ("grupo_5", "Grupo 5", "Camina y sube sin ayuda, agarre en una mano, sin prótesis de pierna"),
        ("grupo_5_1", "Grupo 5.1", "Camina y sube sin ayuda, agarre en ambas manos, con prótesis de pierna (bajo rodilla o ambas)"),
        ("grupo_5_2", "Grupo 5.2", "Camina y sube sin ayuda, con prótesis por encima de la rodilla"),
        ("grupo_10", "Grupo 10", "Discapacidad certificada por alteración del sistema nervioso o trastorno mental severo"),
        ("protesis_mano", "Prótesis de mano", "Tiene prótesis en una o ambas manos"),
        ("protesis_pierna_arriba_rodilla", "Prótesis de pierna sobre rodilla", "Tiene prótesis de pierna por encima de la rodilla"),
    ];

    private static readonly (string Codigo, string Nombre)[] CondicionesCatalogo =
    [
        ("discapacidad_visual", "Discapacidad visual"),
        ("discapacidad_auditiva", "Discapacidad auditiva"),
        ("discapacidad_cognitiva", "Discapacidad cognitiva"),
        ("embarazo", "Embarazo"),
        ("problemas_cardiacos", "Problemas cardíacos"),
        ("problemas_columna", "Problemas de columna"),
        ("mareo_vertigo", "Mareo / vértigo"),
        ("miedo_espacios_cerrados", "Miedo a espacios cerrados"),
    ];

    // Las 10 preguntas del test guiado, portadas tal cual de TestViewModel.MapAnswersToRestrictions
    // (AppParque/Features/EvaluacionAccesibilidad) — mismo texto y mismas opciones que el prototipo,
    // con un Codigo estable por opción que es lo que consume MotorReglas (no el texto ni la posición).
    // "cond_cirugia_reciente" y "cond_ninguna" no activan ningún grupo/condición: así era en el
    // motor original (esas dos opciones nunca tuvieron efecto), se preserva tal cual.
    private static readonly (string Texto, TipoPregunta Tipo, (string Texto, string Codigo)[] Opciones)[] PreguntasCatalogo =
    [
        ("¿Puede caminar sin ningún tipo de apoyo como bastón, muletas o prótesis?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "camina_sin_apoyo"),
            ("No", "no_camina"),
        ]),
        ("¿Tiene prótesis en las piernas?", TipoPregunta.UnicaSeleccion,
        [
            ("No", "sin_protesis_pierna"),
            ("Sí, por debajo de rodilla", "protesis_pierna_debajo_rodilla"),
            ("Sí, por encima de rodilla", "protesis_pierna_arriba_rodilla"),
            ("Ambas", "protesis_pierna_ambas"),
        ]),
        ("¿Puede subir y bajar escaleras sin ayuda?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "sube_sin_ayuda"),
            ("Sí, con ayuda (bastón/muleta)", "sube_con_ayuda"),
            ("No", "no_sube"),
        ]),
        ("¿Tiene capacidad de agarre en ambas manos?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "agarre_ambas_manos"),
            ("Solo una mano", "agarre_una_mano"),
            ("Ninguna", "sin_agarre"),
        ]),
        ("¿Tiene alguna ausencia o prótesis de brazo o mano?", TipoPregunta.UnicaSeleccion,
        [
            ("No", "sin_protesis_mano"),
            ("Prótesis en una mano", "protesis_mano_una"),
            ("Prótesis en ambas manos", "protesis_mano_ambas"),
        ]),
        ("¿Tiene discapacidad visual total?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_visual_total"),
            ("No", "sin_discapacidad_visual"),
            ("Parcial", "discapacidad_visual_parcial"),
        ]),
        ("¿Tiene discapacidad auditiva total?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_auditiva_si"),
            ("No", "discapacidad_auditiva_no"),
        ]),
        ("¿Tiene alguna condición cognitiva diagnosticada o dificultad para entender instrucciones complejas?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "discapacidad_cognitiva_si"),
            ("No", "discapacidad_cognitiva_no"),
        ]),
        ("¿Tiene una discapacidad certificada por alteración del sistema nervioso o trastornos mentales severos?", TipoPregunta.UnicaSeleccion,
        [
            ("Sí", "grupo10_si"),
            ("No", "grupo10_no"),
        ]),
        ("¿Tiene actualmente alguna de las siguientes condiciones médicas? (Marca todas las que apliquen)", TipoPregunta.SeleccionMultiple,
        [
            ("Embarazo", "cond_embarazo"),
            ("Problemas cardíacos, presión alta, marcapasos", "cond_cardiacos"),
            ("Cirugías recientes, yesos o lesiones", "cond_cirugia_reciente"),
            ("Problemas de cuello, columna o huesos", "cond_columna"),
            ("Mareo, vértigo o miedo a las alturas", "cond_mareo_vertigo"),
            ("Miedo a espacios cerrados", "cond_miedo_espacios_cerrados"),
            ("Ninguna", "cond_ninguna"),
        ]),
    ];

    public static async Task SeedAsync(AppDbContext db, string contentRootPath)
    {
        if (await db.Usuarios.AnyAsync())
            return;

        var grupos = GruposCatalogo
            .Select(g => new Grupo { Codigo = g.Codigo, Nombre = g.Nombre, Descripcion = g.Descripcion })
            .ToList();
        db.Grupos.AddRange(grupos);

        var condiciones = CondicionesCatalogo
            .Select(c => new Condicion { Codigo = c.Codigo, Nombre = c.Nombre })
            .ToList();
        db.Condiciones.AddRange(condiciones);

        var ordenPregunta = 1;
        foreach (var p in PreguntasCatalogo)
        {
            var pregunta = new Pregunta { Texto = p.Texto, Tipo = p.Tipo, Orden = ordenPregunta++, Activa = true };

            var ordenOpcion = 1;
            foreach (var (textoOpcion, codigo) in p.Opciones)
                pregunta.Opciones.Add(new OpcionRespuesta { Texto = textoOpcion, Codigo = codigo, Orden = ordenOpcion++ });

            db.Preguntas.Add(pregunta);
        }

        await db.SaveChangesAsync();

        var jsonPath = Path.Combine(contentRootPath, "Data", "Seed", "admin.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        var seed = JsonSerializer.Deserialize<SeedRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new InvalidOperationException($"No se pudo leer el archivo de datos semilla: {jsonPath}");

        foreach (var user in seed.Users.Values)
        {
            db.Usuarios.Add(new Usuario
            {
                NombreCompleto = user.NurseName,
                Username = user.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                Rol = user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? RolUsuario.Admin : RolUsuario.Enfermero,
                Activo = true,
            });
        }

        var grupoPorCodigo = grupos.ToDictionary(g => g.Codigo);
        var condicionPorCodigo = condiciones.ToDictionary(c => c.Codigo);

        foreach (var a in seed.Atracciones.Values)
        {
            var atraccion = new Atraccion
            {
                Nombre = a.Nombre,
                Descripcion = a.Descripcion,
                ImagenUrl = a.ImagenUrl,
                AlturaMinima = a.AlturaMinima,
                AlturaMaxima = a.AlturaMaxima,
                Activa = true,
                Intensidad = string.IsNullOrWhiteSpace(a.Intensidad) ? "media" : a.Intensidad,
            };

            foreach (var (codigo, permitido) in a.Restricciones.Grupos)
            {
                if (grupoPorCodigo.TryGetValue(codigo, out var grupo))
                    atraccion.RestriccionesGrupo.Add(new AtraccionGrupoRestriccion { Grupo = grupo, Permitido = permitido });
            }

            foreach (var (codigo, permitido) in a.Restricciones.Condiciones)
            {
                if (condicionPorCodigo.TryGetValue(codigo, out var condicion))
                    atraccion.RestriccionesCondicion.Add(new AtraccionCondicionRestriccion { Condicion = condicion, Permitido = permitido });
            }

            db.Atracciones.Add(atraccion);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Carga inicial de Modules/CatalogoZonas, portada tal cual de
    /// AppParque/Features/EvaluacionAccesibilidad/CatalogoZonas.cs (cliente) — mismas 10 zonas,
    /// mismas preguntas, mismas opciones y mismos valores "sin novedad", para que el test se
    /// comporte exactamente igual el día que el cliente empiece a leerlo de la API en vez de
    /// traerlo hardcodeado. Independiente de <see cref="SeedAsync"/> (que solo corre si no hay
    /// usuarios) porque esta base de datos ya tiene usuarios — se guarda sola con su propia
    /// bandera de "ya corrí" (que no haya zonas todavía).
    /// </summary>
    public static async Task SeedZonasCapacidadesAsync(AppDbContext db)
    {
        if (await db.ZonasCorporales.AnyAsync())
            return;

        var siNo = new[] { ("Sí", "Si"), ("No", "No") };
        var niveles = new[] { ("Normal", "Normal"), ("Parcial", "Parcial"), ("Pérdida total", "Total") };

        static OpcionCapacidad O(string texto, string valor, int orden) => new() { Texto = texto, Valor = valor, Orden = orden };

        List<OpcionCapacidad> Opciones((string Texto, string Valor)[] pares) =>
            pares.Select((p, i) => O(p.Texto, p.Valor, i)).ToList();

        var zonas = new List<ZonaCorporal>();
        var orden = 1;

        // --- Cabeza --------------------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "cabeza", Nombre = "Cabeza", Corto = "Cabeza", EnCuerpo = true, Orden = orden++,
            Nota = "Aquí se revisan visión, audición, condición cognitiva y comprensión de instrucciones.",
            ResumenSinNovedad = "Ve, oye y comprende las instrucciones sin dificultad",
            Preguntas =
            [
                new() { Clave = "vision", Titulo = "¿Cómo es su visión?", Orden = 1,
                    Ayuda = "10 atracciones no admiten visión total comprometida.",
                    Opciones = Opciones(niveles), ValorSinNovedad = "Normal" },
                new() { Clave = "audicion", Titulo = "¿Cómo es su audición?", Orden = 2,
                    Ayuda = "Solo Paseo a Caballo la exige: el jinete debe oír las indicaciones del guía.",
                    Opciones = Opciones(niveles), ValorSinNovedad = "Normal" },
                new() { Clave = "cognitivo", Titulo = "¿Tiene alguna condición cognitiva?", Orden = 3, Eje = 1,
                    Ayuda = "Marque certificada solo con diagnóstico documentado. 6 atracciones no admiten ninguna condición cognitiva y 17 piden acompañante adulto.",
                    Opciones = Opciones([("Ninguna", "Ninguna"), ("Leve", "Leve"), ("Severa certificada", "SeveraCertificada")]),
                    ValorSinNovedad = "Ninguna" },
                new() { Clave = "comprende", Titulo = "¿Comprende y sigue las instrucciones de seguridad?", Orden = 4, Eje = 3,
                    Ayuda = "Es una pregunta aparte del diagnóstico: mantenerse sentado, no soltarse y seguir la indicación de embarque.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "Si" },
            ],
        });

        // --- Brazo y mano izquierda / derecha --------------------------------------------------
        ZonaCorporal BrazoYMano(string codigo, string nombre, string corto, string lado) => new()
        {
            Codigo = codigo, Nombre = nombre, Corto = corto, EnCuerpo = true, Orden = orden++,
            Nota = "Estado de la mano, capacidad de agarre, acortamiento y prótesis.",
            ResumenSinNovedad = "Mano íntegra, agarre funcional, sin acortamiento ni prótesis",
            Preguntas =
            [
                new() { Clave = "estado", Titulo = $"¿Cómo está la mano {lado}?", Orden = 1, Eje = 1,
                    Opciones = Opciones([("Íntegra", "Integra"), ("Disfuncional", "Disfuncional"), ("Amputación", "Amputacion")]),
                    ValorSinNovedad = "Integra" },
                new() { Clave = "agarre", Titulo = "¿Puede sujetarse con esta mano?", Orden = 2, Eje = 3,
                    Ayuda = "17 atracciones exigen agarre en una mano; 3 lo exigen en ambas.",
                    Opciones = Opciones([("Agarre funcional", "Funcional"), ("Agarre limitado", "Limitado"), ("No se sujeta", "Ninguno")]),
                    ValorSinNovedad = "Funcional" },
                new() { Clave = "acortamiento", Titulo = "¿Hay acortamiento del brazo?", Orden = 3,
                    Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "protesisMano", Titulo = "¿Usa prótesis de mano?", Orden = 4, Eje = 2,
                    Ayuda = "11 atracciones no la permiten.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "removible", Titulo = "¿Se la puede quitar?", Orden = 5,
                    Ayuda = "Se registra y el personal lo ve en el dictamen, pero todavía no cambia el resultado: el uso sin prótesis está pendiente de aprobación.",
                    Opciones = Opciones(siNo), DependeDeClave = "protesisMano", ActivaCuandoCsv = "Si" },
            ],
        };
        zonas.Add(BrazoYMano("brazoIzq", "Brazo y mano izquierda", "Brazo izq", "izquierda"));
        zonas.Add(BrazoYMano("brazoDer", "Brazo y mano derecha", "Brazo der", "derecha"));

        // --- Tronco y postura ------------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "tronco", Nombre = "Tronco y postura", Corto = "Tronco", EnCuerpo = true, Orden = orden++,
            Nota = "Sedestación, flexión de piernas y las dos condiciones del torso.",
            ResumenSinNovedad = "Se sienta erguido solo, dobla las piernas, sin problemas de columna ni cardíacos",
            Preguntas =
            [
                new() { Clave = "sedestacion", Titulo = "¿Se mantiene sentado erguido?", Orden = 1, Eje = 3,
                    Ayuda = "25 atracciones lo exigen. Los dos carruseles piden la variante «durante todo el recorrido».",
                    Opciones = Opciones([("Sí, todo el recorrido", "AutonomaTodoElRecorrido"), ("Sí, por sí solo", "Autonoma"), ("Solo con apoyo", "ConApoyo"), ("No puede", "No")]),
                    ValorSinNovedad = "AutonomaTodoElRecorrido" },
                new() { Clave = "flexion", Titulo = "¿Puede doblar las piernas para sentarse?", Orden = 2, Eje = 3,
                    Ayuda = "18 atracciones lo exigen para cerrar la barra de seguridad.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "Si" },
                new() { Clave = "osteomuscular", Titulo = "¿Tiene problemas de cuello, columna o huesos?", Orden = 3,
                    Ayuda = "Restringe 19 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "cardiaco", Titulo = "¿Problemas cardíacos, tensión arterial o marcapasos?", Orden = 4,
                    Ayuda = "Restringe 9 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
            ],
        });

        // --- Pierna izquierda / derecha ---------------------------------------------------------
        ZonaCorporal Pierna(string codigo, string nombre, string corto, string lado) => new()
        {
            Codigo = codigo, Nombre = nombre, Corto = corto, EnCuerpo = true, Orden = orden++,
            Nota = "Estado, prótesis, rodilla completa y pie funcional.",
            ResumenSinNovedad = "Pierna íntegra, rodilla completa, pie funcional, sin prótesis",
            Preguntas =
            [
                new() { Clave = "estado", Titulo = $"¿Cómo está la pierna {lado}?", Orden = 1, Eje = 1,
                    Opciones = Opciones([("Íntegra", "Integra"), ("Disfuncional", "Disfuncional"), ("Amputación bajo rodilla", "AmputacionBajoRodilla"), ("Amputación sobre rodilla", "AmputacionSobreRodilla")]),
                    ValorSinNovedad = "Integra" },
                new() { Clave = "protesis", Titulo = "¿Usa prótesis en esta pierna?", Orden = 2, Eje = 2,
                    Ayuda = "17 atracciones no permiten prótesis por encima de la rodilla. La ficha restringe el aparato, no el muñón.",
                    Opciones = Opciones([("No usa", "Sin"), ("Sí, bajo rodilla", "BajoRodilla"), ("Sí, sobre rodilla", "SobreRodilla")]),
                    ValorSinNovedad = "Sin" },
                new() { Clave = "removible", Titulo = "¿Se la puede quitar?", Orden = 3,
                    Ayuda = "Se registra y el personal lo ve en el dictamen, pero todavía no cambia el resultado: el uso sin prótesis está pendiente de aprobación.",
                    Opciones = Opciones(siNo), DependeDeClave = "protesis", ActivaCuandoCsv = "BajoRodilla,SobreRodilla" },
                new() { Clave = "rodilla", Titulo = "¿La barra de seguridad puede sujetar esta rodilla?", Orden = 4, Eje = 3,
                    Ayuda = "10 fichas piden rodilla completa. Es un requisito del mecanismo, no un diagnóstico.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "Si" },
                new() { Clave = "pie", Titulo = "¿El pie es funcional?", Orden = 5, Eje = 3,
                    Ayuda = "Apoya el pie en el estribo o el reposapiés.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "Si" },
            ],
        };
        zonas.Add(Pierna("piernaIzq", "Pierna izquierda", "Pierna izq", "izquierda"));
        zonas.Add(Pierna("piernaDer", "Pierna derecha", "Pierna der", "derecha"));

        // --- Marcha y embarque -------------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "marcha", Nombre = "Marcha y embarque", Corto = "Marcha y embarque", EnCuerpo = true, Orden = orden++,
            Nota = "Cómo se desplaza y cómo sube a la atracción.",
            ResumenSinNovedad = "Camina sin apoyo, sube escaleras y embarca por sí sola",
            Preguntas =
            [
                new() { Clave = "capacidad", Titulo = "¿Cómo se desplaza?", Orden = 1, Eje = 3,
                    Ayuda = "12 atracciones admiten bastón o muletas; 7 exigen caminar sobre los dos pies sin ningún apoyo.",
                    Opciones = Opciones([("No camina", "NoCamina"), ("Camina con apoyo", "ConApoyo"), ("Camina sin apoyo", "SinApoyo")]),
                    ValorSinNovedad = "SinApoyo" },
                new() { Clave = "escaleras", Titulo = "¿Sube y baja escaleras?", Orden = 2, Eje = 3,
                    Opciones = Opciones([("No", "NoCamina"), ("Con apoyo", "ConApoyo"), ("Sin apoyo", "SinApoyo")]),
                    ValorSinNovedad = "SinApoyo" },
                new() { Clave = "embarque", Titulo = "¿Entra, embarca y sale por sí sola?", Orden = 3, Eje = 3,
                    Ayuda = "Varias fichas lo exigen; otras aceptan que la asista el acompañante adulto.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "Si" },
                new() { Clave = "silla", Titulo = "¿Usa silla de ruedas?", Orden = 4,
                    Ayuda = "Solo 4 atracciones tienen cabina accesible.",
                    Opciones = Opciones([("No usa", "NoAplica"), ("Se transfiere sola", "TransferenciaAutonoma"), ("Se transfiere con ayuda", "TransferenciaConAyuda"), ("No se transfiere", "NoSeTransfiere")]),
                    ValorSinNovedad = "NoAplica" },
            ],
        });

        // --- Medidas -----------------------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "medidas", Nombre = "Medidas", Corto = "Medidas", EnCuerpo = false, Orden = orden++,
            Nota = "Descartan atracciones de forma automática, antes de llegar a la fila.",
            ResumenSinNovedad = "", PermiteSinNovedad = false,
            Preguntas =
            [
                new() { Clave = "estatura", Titulo = "¿Cuánto mide?", Orden = 1, Tipo = TipoRespuestaCapacidad.Numero, Unidad = "cm",
                    Ayuda = "Es el umbral niño/adulto del parque: 110, 120, 130, 140 o 150 cm según la atracción." },
                new() { Clave = "peso", Titulo = "¿Cuánto pesa?", Orden = 2, Tipo = TipoRespuestaCapacidad.Numero, Unidad = "kg",
                    Ayuda = "Solo Paseo a Caballo declara límite: 100 kg." },
                new() { Clave = "edad", Titulo = "¿Qué edad tiene?", Orden = 3, Tipo = TipoRespuestaCapacidad.Numero, Unidad = "años" },
            ],
        });

        // --- Condiciones generales -----------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "generales", Nombre = "Condiciones generales", Corto = "Generales", EnCuerpo = false, Orden = orden++,
            Nota = "Las que no viven en una parte del cuerpo. Son independientes entre sí.",
            ResumenSinNovedad = "Ninguna de las 8 condiciones",
            Preguntas =
            [
                new() { Clave = "embarazo", Titulo = "¿Está en embarazo?", Orden = 1, Ayuda = "Restringe 16 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "cirugia", Titulo = "¿Cirugías recientes o yesos?", Orden = 2, Ayuda = "Restringe 19 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "cuidado", Titulo = "¿Está bajo cuidado médico?", Orden = 3, Ayuda = "Restringe 19 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "convulsiones", Titulo = "¿Antecedentes convulsivos?", Orden = 4, Ayuda = "Restringe 11 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "mental", Titulo = "¿Algún trastorno mental?", Orden = 5, Ayuda = "Restringe 15 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "mareo", Titulo = "¿Sufre de mareo o vértigo?", Orden = 6, Ayuda = "Restringe 19 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "alturas", Titulo = "¿Le teme a las alturas?", Orden = 7,
                    Ayuda = "Restringe 13 atracciones. Va aparte del mareo: la Rueda Panorámica las lista separadas.",
                    Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "claustrofobia", Titulo = "¿Le teme a los espacios cerrados?", Orden = 8, Ayuda = "Restringe 4 atracciones.", Opciones = Opciones(siNo), ValorSinNovedad = "No" },
            ],
        });

        // --- Acompañante y rol -----------------------------------------------------------------
        zonas.Add(new ZonaCorporal
        {
            Codigo = "contexto", Nombre = "Acompañante y rol", Corto = "Acompañante", EnCuerpo = false, Orden = orden++,
            Nota = "El acompañante tiene requisitos propios: es un segundo perfil a evaluar, no una casilla.",
            ResumenSinNovedad = "Único ocupante, sin acompañante",
            Preguntas =
            [
                new() { Clave = "rol", Titulo = "¿Qué rol va a tomar?", Orden = 1,
                    Ayuda = "Karts Dobles y Carros Chocones piden requisitos distintos para conductor y pasajero.",
                    Opciones = Opciones([("Único ocupante", "Unico"), ("Conductor", "Conductor"), ("Pasajero", "Pasajero")]),
                    ValorSinNovedad = "Unico" },
                new() { Clave = "presente", Titulo = "¿Viene con acompañante?", Orden = 2, Opciones = Opciones(siNo), ValorSinNovedad = "No" },
                new() { Clave = "adulto", Titulo = "¿El acompañante es adulto o mide más de 150 cm?", Orden = 3,
                    Opciones = Opciones(siNo), DependeDeClave = "presente", ActivaCuandoCsv = "Si" },
                new() { Clave = "embarazoAcomp", Titulo = "¿El acompañante está en embarazo?", Orden = 4,
                    Ayuda = "Los dos carruseles lo restringen: el acompañante adulto no debe estar embarazado.",
                    Opciones = Opciones(siNo), DependeDeClave = "presente", ActivaCuandoCsv = "Si" },
            ],
        });

        db.ZonasCorporales.AddRange(zonas);
        await db.SaveChangesAsync();
    }

    private sealed class SeedRoot
    {
        public Dictionary<string, SeedUser> Users { get; set; } = new();
        public Dictionary<string, SeedAtraccion> Atracciones { get; set; } = new();
    }

    private sealed class SeedUser
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string NurseName { get; set; } = null!;
    }

    private sealed class SeedAtraccion
    {
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }

        [JsonPropertyName("altura_minima")]
        public int? AlturaMinima { get; set; }

        [JsonPropertyName("altura_maxima")]
        public int? AlturaMaxima { get; set; }

        [JsonPropertyName("imagenURL")]
        public string? ImagenUrl { get; set; }

        /// <summary>baja | media | alta | extrema | cultural. Opcional: si la ficha del seed no lo
        /// declara, la atracción queda en "media".</summary>
        public string? Intensidad { get; set; }

        public SeedRestricciones Restricciones { get; set; } = new();
    }

    private sealed class SeedRestricciones
    {
        public Dictionary<string, bool> Grupos { get; set; } = new();
        public Dictionary<string, bool> Condiciones { get; set; } = new();
    }
}
