namespace AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

/// <summary>
/// Los valores que puede tomar cada dimensión del vocabulario de capacidades. Reemplazan a los
/// 13 grupos del modelo anterior (grupo_1 … grupo_10): un grupo era una foto del visitante y no
/// se podía descomponer; una dimensión sí.
///
/// Regla que ordena todo el vocabulario (ver Requierements/Estrategia-Capacidades-Atomicas.md):
///   · <b>estado</b>    — lo que es el cuerpo. No cambia durante la visita.
///   · <b>dispositivo</b> — la ayuda (prótesis, órtesis, bastón), aparte del cuerpo y con su propia
///     removibilidad. Las atracciones restringen el <i>dispositivo</i>, no el estado del cuerpo.
///   · <b>capacidad</b>  — lo que exige el dispositivo de seguridad de la atracción. Es consecuencia
///     de los dos anteriores, pero las atracciones la piden por su nombre.
/// </summary>
public enum Respuesta
{
    /// <summary>Sin dato. Nunca equivale a "no": no permite por omisión ni restringe por omisión,
    /// obliga a preguntar. Es el valor por defecto a propósito (0).</summary>
    NoDeterminado = 0,
    Si,
    No,
}

/// <summary>Autonomía de marcha. H-04: lo que se evalúa es la capacidad, no el tipo de ayuda.</summary>
public enum CapacidadMarcha
{
    NoDeterminado = 0,
    /// <summary>No se desplaza caminando (usa silla de ruedas para desplazarse).</summary>
    NoCamina,
    /// <summary>Camina apoyándose en bastón, muletas, caminador o acompañante.</summary>
    ConApoyo,
    /// <summary>Camina sobre los dos pies sin ningún tipo de apoyo.</summary>
    SinApoyo,
}

/// <summary>
/// Ayuda con la que el visitante se desplaza. H-04: se registra como contexto operativo y
/// para la hoja de ruta del personal — <b>ninguna regla del motor pregunta por el tipo de ayuda</b>,
/// solo por <see cref="CapacidadMarcha"/>. Caminador y silla de ruedas siguen siendo apoyos.
/// </summary>
public enum ApoyoMarcha
{
    Baston,
    Muletas,
    Caminador,
    SillaRuedas,
    Acompanante,
}

public enum EstadoPierna
{
    NoDeterminado = 0,
    Integra,
    Disfuncional,
    AmputacionBajoRodilla,
    AmputacionSobreRodilla,
}

public enum NivelProtesis
{
    BajoRodilla,
    SobreRodilla,
}

public enum CapacidadAgarre
{
    NoDeterminado = 0,
    Ninguno,
    Limitado,
    Funcional,
}

public enum EstadoMano
{
    NoDeterminado = 0,
    Integra,
    Disfuncional,
    Amputacion,
}

/// <summary>Sedestación erguida. Los carruseles exigen la variante "durante todo el recorrido",
/// que es más estricta que "puede sentarse erguido".</summary>
public enum SedestacionErguida
{
    NoDeterminado = 0,
    No,
    ConApoyo,
    Autonoma,
    AutonomaTodoElRecorrido,
}

/// <summary>Cómo se relaciona el visitante usuario de silla de ruedas con la atracción.</summary>
public enum UsoSillaRuedas
{
    /// <summary>No es usuario de silla de ruedas.</summary>
    NoAplica = 0,
    /// <summary>Puede transferirse a la cabina/silla por sí solo.</summary>
    TransferenciaAutonoma,
    /// <summary>Se transfiere con ayuda de un acompañante.</summary>
    TransferenciaConAyuda,
    /// <summary>No se transfiere; solo puede permanecer en su silla, estable.</summary>
    PermanenciaEstable,
    /// <summary>No se transfiere ni se mantiene estable sentado.</summary>
    NoSeTransfiere,
}

/// <summary>Rol dentro de la atracción. Karts dobles y Carros chocones tienen requisitos distintos
/// por rol, no una sola lista.</summary>
public enum RolVisitante
{
    Unico = 0,
    Conductor,
    Pasajero,
}
