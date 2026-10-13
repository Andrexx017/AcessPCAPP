namespace AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

/// <summary>
/// El visitante descrito en dimensiones atómicas. Es la entrada del motor
/// (<see cref="Application.MotorCapacidades"/>) y lo que el test guiado debe llenar.
///
/// Nada aquí menciona atracciones: el perfil describe a la persona, los requisitos describen
/// a la atracción, y el motor es lo único que los cruza. Ese corte es lo que permite agregar
/// una atracción sin tocar el perfil, y agregar una pregunta sin tocar las 29 atracciones.
/// </summary>
public record PerfilCapacidades
{
    public Lateralidad<Pierna> Piernas { get; init; } = new(new Pierna(), new Pierna());
    public Lateralidad<Mano> Manos { get; init; } = new(new Mano(), new Mano());
    public Marcha Marcha { get; init; } = new();
    public Postura Postura { get; init; } = new();
    public Embarque Embarque { get; init; } = new();
    public Antropometria Cuerpo { get; init; } = new();
    public Sensorial Sensorial { get; init; } = new();
    public Salud Salud { get; init; } = new();
    public Contexto Contexto { get; init; } = new();
}

/// <summary>Evita duplicar 17 campos "izq"/"der". Las reglas preguntan por conteos
/// ("al menos una", "ambas"), nunca por un lado concreto.</summary>
public record Lateralidad<T>(T Izquierda, T Derecha)
{
    public IEnumerable<T> Ambas => [Izquierda, Derecha];

    public int Cuenta(Func<T, bool> cumple) => Ambas.Count(cumple);
}

public record Pierna
{
    public EstadoPierna Estado { get; init; }

    /// <summary>null = no usa prótesis en esta pierna. La prótesis es un objeto aparte del cuerpo
    /// precisamente porque se puede poner y quitar.</summary>
    public Protesis? Protesis { get; init; }

    /// <summary>Requisito mecánico de la barra de seguridad, no una propiedad clínica: que el
    /// dispositivo pueda sujetar una pierna con rodilla completa.</summary>
    public Respuesta RodillaCompleta { get; init; }

    public Respuesta PieFuncional { get; init; }
}

/// <summary>
/// H-03 (pendiente de reunión con las partes interesadas): <see cref="Removible"/> se captura y se
/// almacena, pero el motor todavía NO genera el veredicto alternativo "sin la prótesis" — solo lo
/// señala como nota informativa. Ver la sección H-03 del documento de estrategia.
/// </summary>
public record Protesis(NivelProtesis Nivel, Respuesta Removible);

public record Mano
{
    public EstadoMano Estado { get; init; }
    public CapacidadAgarre Agarre { get; init; }
    public Respuesta Acortamiento { get; init; }
    public Protesis? Protesis { get; init; }
}

public record Marcha
{
    public CapacidadMarcha Capacidad { get; init; }

    /// <summary>Contexto operativo, no insumo de reglas (H-04).</summary>
    public IReadOnlyList<ApoyoMarcha> Apoyos { get; init; } = [];

    /// <summary>Hay atracciones que admiten muletas al caminar pero no al subir escaleras,
    /// así que escaleras es su propia dimensión.</summary>
    public CapacidadMarcha Escaleras { get; init; }
}

public record Postura
{
    public SedestacionErguida SedestacionErguida { get; init; }
    public Respuesta FlexionPiernas { get; init; }

    /// <summary>Juicio del operador en sitio, no un hecho deducible. Si queda
    /// <see cref="Respuesta.NoDeterminado"/>, el motor devuelve "requiere verificación presencial"
    /// en las atracciones que lo exigen, en vez de inventar un sí o un no.</summary>
    public Respuesta AcomodacionSegura { get; init; }
}

public record Embarque
{
    public Respuesta Autonomo { get; init; }
    public UsoSillaRuedas SillaRuedas { get; init; }
}

public record Antropometria
{
    /// <summary>H-11: la frontera niño/adulto del parque es la estatura, no la edad, y cada
    /// atracción tiene su propio umbral (110, 120, 130, 140 o 150 cm).</summary>
    public int? EstaturaCm { get; init; }

    /// <summary>H-07: solo Paseo a Caballo declara límite (100 kg), pero se pregunta siempre para
    /// descartar automáticamente en vez de en la fila.</summary>
    public int? PesoKg { get; init; }

    public int? EdadAnios { get; init; }
}

public record Sensorial
{
    public NivelVision Vision { get; init; }
    public NivelAudicion Audicion { get; init; }
    public NivelCognitivo Cognitivo { get; init; }

    /// <summary>H-05: se pregunta de forma propia. Toda atracción con barra de seguridad la asume
    /// (mantenerse sentado, no soltarse, seguir la indicación de embarque) y "discapacidad
    /// cognitiva" no es lo mismo que no comprender instrucciones.</summary>
    public Respuesta ComprendeInstrucciones { get; init; }
}

public enum NivelVision { NoDeterminado = 0, Normal, Parcial, Total }

public enum NivelAudicion { NoDeterminado = 0, Normal, Parcial, Total }

public enum NivelCognitivo { NoDeterminado = 0, Ninguna, Leve, SeveraCertificada }

/// <summary>
/// Condiciones de salud, independientes entre sí y de la movilidad — por eso nunca debieron
/// compartir numeración con los grupos. Los códigos de <see cref="CodigosCondicion"/> son los que
/// usan los requisitos de cada atracción.
/// </summary>
public record Salud
{
    public Respuesta Embarazo { get; init; }
    public int? SemanasEmbarazo { get; init; }
    public Respuesta Cardiaco { get; init; }
    public Respuesta Osteomuscular { get; init; }
    public Respuesta CirugiaReciente { get; init; }
    public Respuesta BajoCuidadoMedico { get; init; }
    public Respuesta Convulsiones { get; init; }
    public Respuesta TrastornoMental { get; init; }
    public Respuesta MareoVertigo { get; init; }
    public Respuesta MiedoAlturas { get; init; }
    public Respuesta Claustrofobia { get; init; }

    public Respuesta Obtener(string codigo) => codigo switch
    {
        CodigosCondicion.Embarazo => Embarazo,
        CodigosCondicion.Cardiaco => Cardiaco,
        CodigosCondicion.Osteomuscular => Osteomuscular,
        CodigosCondicion.CirugiaReciente => CirugiaReciente,
        CodigosCondicion.BajoCuidadoMedico => BajoCuidadoMedico,
        CodigosCondicion.Convulsiones => Convulsiones,
        CodigosCondicion.TrastornoMental => TrastornoMental,
        CodigosCondicion.MareoVertigo => MareoVertigo,
        CodigosCondicion.MiedoAlturas => MiedoAlturas,
        CodigosCondicion.Claustrofobia => Claustrofobia,
        _ => Respuesta.NoDeterminado,
    };
}

/// <summary>
/// Códigos de condición médica usados en <c>restriccionesMedicas</c> de
/// Data/Seed/requisitos-atracciones.json. Son los que aparecen literalmente en las fichas de
/// seguridad del parque; mareo/vértigo y miedo a las alturas van separados porque hay atracciones
/// (Rueda Panorámica) que los listan como restricciones distintas.
/// </summary>
public static class CodigosCondicion
{
    public const string Embarazo = "embarazo";
    public const string Cardiaco = "cardiaco";
    public const string Osteomuscular = "osteomuscular";
    public const string CirugiaReciente = "cirugia_reciente";
    public const string BajoCuidadoMedico = "bajo_cuidado_medico";
    public const string Convulsiones = "convulsiones";
    public const string TrastornoMental = "trastorno_mental";
    public const string MareoVertigo = "mareo_vertigo";
    public const string MiedoAlturas = "miedo_alturas";
    public const string Claustrofobia = "claustrofobia";

    public static readonly IReadOnlyDictionary<string, string> Etiquetas = new Dictionary<string, string>
    {
        [Embarazo] = "embarazo",
        [Cardiaco] = "problemas cardíacos, tensión arterial o marcapasos",
        [Osteomuscular] = "problemas de cuello, columna o huesos",
        [CirugiaReciente] = "cirugías recientes o yesos",
        [BajoCuidadoMedico] = "estar bajo cuidado médico",
        [Convulsiones] = "antecedentes convulsivos",
        [TrastornoMental] = "trastorno mental",
        [MareoVertigo] = "mareo o vértigo",
        [MiedoAlturas] = "miedo a las alturas",
        [Claustrofobia] = "claustrofobia o miedo a espacios cerrados",
    };
}

public record Contexto
{
    public RolVisitante Rol { get; init; }

    /// <summary>El acompañante tiene requisitos propios (los carruseles exigen que no esté
    /// embarazado), así que es un segundo perfil a evaluar, no una casilla.</summary>
    public PerfilAcompanante? Acompanante { get; init; }
}

/// <summary>
/// H-10 / H-11: el acompañante válido es "adulto" o una persona de estatura superior al umbral que
/// fije la atracción (150 cm en casi todas). Solo se piden los datos que alguna atracción llega a
/// verificar sobre él.
/// </summary>
public record PerfilAcompanante
{
    public bool Presente { get; init; }
    public bool EsAdulto { get; init; }
    public int? EstaturaCm { get; init; }
    public Respuesta Embarazo { get; init; }
    public Respuesta PuedeAsistirEmbarque { get; init; }
}
