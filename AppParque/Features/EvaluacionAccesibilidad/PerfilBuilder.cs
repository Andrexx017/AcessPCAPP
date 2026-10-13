using AppParque.Shared.Models;

namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Traduce las respuestas del recorrido por zonas (claves "zona.pregunta") al
/// <see cref="PerfilCapacidadesDto"/> que espera POST /api/diagnostico.
///
/// Es el único punto donde las zonas del mapa se convierten en dimensiones del vocabulario: el mapa
/// es una forma de agrupar preguntas para el enfermero, no una estructura del modelo. Si mañana la
/// cabeza se parte en dos zonas, cambia <see cref="CatalogoZonas"/> y esta clase, y nada más.
///
/// Lo que no se responde se queda en null, que la API lee como NoDeterminado: ni permite ni
/// restringe, obliga a verificar. Nunca se rellena con un valor "seguro" por omisión.
/// </summary>
public static class PerfilBuilder
{
    public static PerfilCapacidadesDto Construir(IReadOnlyDictionary<string, string> respuestas)
    {
        string? V(string clave) => respuestas.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor)
            ? valor
            : null;

        int? N(string clave) => int.TryParse(V(clave), out var numero) ? numero : null;

        return new PerfilCapacidadesDto
        {
            Piernas = new LateralidadDto<PiernaDto>(Pierna("piernaIzq"), Pierna("piernaDer")),
            Manos = new LateralidadDto<ManoDto>(Mano("brazoIzq"), Mano("brazoDer")),

            Marcha = new MarchaDto
            {
                Capacidad = V("marcha.capacidad"),
                Escaleras = V("marcha.escaleras"),
                // Los apoyos son contexto operativo y ninguna regla del motor los usa (H-04), así
                // que se derivan de la silla de ruedas en vez de preguntarse aparte.
                Apoyos = V("marcha.silla") is "TransferenciaAutonoma" or "TransferenciaConAyuda" or "NoSeTransfiere"
                    ? ["SillaRuedas"]
                    : [],
            },

            Postura = new PosturaDto
            {
                SedestacionErguida = V("tronco.sedestacion"),
                FlexionPiernas = V("tronco.flexion"),
                // No se pregunta: la ficha de Avix la deja al juicio del operador en sitio, así que
                // se manda sin dato a propósito y el motor devuelve "verificar en la atracción".
                AcomodacionSegura = null,
            },

            Embarque = new EmbarqueDto
            {
                Autonomo = V("marcha.embarque"),
                SillaRuedas = V("marcha.silla"),
            },

            Cuerpo = new AntropometriaDto
            {
                EstaturaCm = N("medidas.estatura"),
                PesoKg = N("medidas.peso"),
                EdadAnios = N("medidas.edad"),
            },

            Sensorial = new SensorialDto
            {
                Vision = V("cabeza.vision"),
                Audicion = V("cabeza.audicion"),
                Cognitivo = V("cabeza.cognitivo"),
                ComprendeInstrucciones = V("cabeza.comprende"),
            },

            Salud = new SaludDto
            {
                Embarazo = V("generales.embarazo"),
                Cardiaco = V("tronco.cardiaco"),
                Osteomuscular = V("tronco.osteomuscular"),
                CirugiaReciente = V("generales.cirugia"),
                BajoCuidadoMedico = V("generales.cuidado"),
                Convulsiones = V("generales.convulsiones"),
                TrastornoMental = V("generales.mental"),
                MareoVertigo = V("generales.mareo"),
                MiedoAlturas = V("generales.alturas"),
                Claustrofobia = V("generales.claustrofobia"),
            },

            Contexto = new ContextoDto
            {
                Rol = V("contexto.rol"),
                Acompanante = V("contexto.presente") is null
                    ? null
                    : new AcompananteDto
                    {
                        Presente = V("contexto.presente") == "Si",
                        EsAdulto = V("contexto.adulto") == "Si",
                        EstaturaCm = null,
                        Embarazo = V("contexto.embarazoAcomp"),
                        PuedeAsistirEmbarque = V("contexto.presente") == "Si" ? "Si" : "No",
                    },
            },
        };

        PiernaDto Pierna(string zona) => new()
        {
            Estado = V($"{zona}.estado"),
            // "Sin" no es un valor del enum del backend: allá la ausencia de prótesis es null,
            // porque la prótesis es un objeto aparte del cuerpo y no un estado de la pierna.
            Protesis = V($"{zona}.protesis") is string nivel && nivel != "Sin"
                ? new ProtesisDto(nivel, V($"{zona}.removible") ?? "NoDeterminado")
                : null,
            RodillaCompleta = V($"{zona}.rodilla"),
            PieFuncional = V($"{zona}.pie"),
        };

        ManoDto Mano(string zona) => new()
        {
            Estado = V($"{zona}.estado"),
            Agarre = V($"{zona}.agarre"),
            Acortamiento = V($"{zona}.acortamiento"),
            // Verruga del modelo: Mano.Protesis reusa el mismo tipo que la pierna, así que exige un
            // NivelProtesis que en una mano no significa nada. El motor nunca lo lee (solo le importa
            // si hay prótesis o no), pero habría que darle a la mano su propio tipo de dispositivo.
            Protesis = V($"{zona}.protesisMano") == "Si"
                ? new ProtesisDto("BajoRodilla", V($"{zona}.removible") ?? "NoDeterminado")
                : null,
        };
    }
}
