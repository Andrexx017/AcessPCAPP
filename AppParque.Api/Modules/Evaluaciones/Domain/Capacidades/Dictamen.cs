namespace AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

/// <summary>
/// El resultado por atracción. El modelo anterior devolvía un booleano
/// (<c>PreseleccionService.EsSeguraPara</c>) y por eso el personal no podía saber por qué una
/// atracción quedaba fuera, ni distinguir "no puede" de "falta preguntarle algo".
/// </summary>
public enum Veredicto
{
    /// <summary>Cumple todos los requisitos.</summary>
    Permitida,

    /// <summary>Cumple si se aplica una mitigación concreta (acompañante, silla del centro…).</summary>
    PermitidaConMitigacion,

    /// <summary>Falta un dato que nadie puede deducir, o la ficha delega en el juicio del operador
    /// (el tamaño físico en Avix). No es un sí ni un no: es "revisar en la atracción".</summary>
    RequiereVerificacionPresencial,

    /// <summary>Incumple un requisito.</summary>
    Restringida,
}

public enum Severidad
{
    /// <summary>No afecta el veredicto; se imprime en la guía del visitante.</summary>
    Informativo,

    /// <summary>Condiciona el acceso a una mitigación.</summary>
    Condicion,

    /// <summary>Hay que verificar en sitio antes de permitir.</summary>
    Verificar,

    /// <summary>Impide el acceso.</summary>
    Bloqueante,
}

/// <summary>
/// Cada motivo dice qué dimensión lo produjo, para que la app pueda llevar al enfermero de vuelta
/// a la pregunta exacta y para que el PDF explique la decisión en lenguaje del visitante.
/// </summary>
public record Motivo(string Codigo, string Dimension, string Texto, Severidad Severidad);

public record DictamenAtraccion
{
    public string CodigoAtraccion { get; init; } = null!;
    public string NombreAtraccion { get; init; } = null!;
    public RolVisitante Rol { get; init; }
    public Veredicto Veredicto { get; init; }
    public IReadOnlyList<Motivo> Motivos { get; init; } = [];

    /// <summary>Mitigaciones que hay que aplicar para que el veredicto
    /// <see cref="Veredicto.PermitidaConMitigacion"/> se sostenga.</summary>
    public IReadOnlyList<string> MitigacionesRequeridas { get; init; } = [];

    public IEnumerable<Motivo> Bloqueantes => Motivos.Where(m => m.Severidad == Severidad.Bloqueante);
}

public record DictamenVisita
{
    public IReadOnlyList<DictamenAtraccion> Atracciones { get; init; } = [];

    /// <summary>Dimensiones del perfil que quedaron en <see cref="Respuesta.NoDeterminado"/> y que
    /// alguna atracción necesitaba. Es la lista de preguntas que falta hacerle al visitante.</summary>
    public IReadOnlyList<string> DimensionesSinDato { get; init; } = [];

    public int Permitidas => Atracciones.Count(a => a.Veredicto == Veredicto.Permitida);
    public int ConMitigacion => Atracciones.Count(a => a.Veredicto == Veredicto.PermitidaConMitigacion);
    public int PorVerificar => Atracciones.Count(a => a.Veredicto == Veredicto.RequiereVerificacionPresencial);
    public int Restringidas => Atracciones.Count(a => a.Veredicto == Veredicto.Restringida);
}
