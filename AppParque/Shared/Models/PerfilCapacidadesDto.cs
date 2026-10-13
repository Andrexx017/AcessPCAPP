namespace AppParque.Shared.Models;

/// <summary>
/// El perfil que viaja a POST /api/diagnostico. Los enums del vocabulario van como <b>texto</b>
/// porque la API los serializa así (JsonStringEnumConverter en Program.cs): el valor de cada
/// string es el nombre exacto del miembro del enum en el backend
/// (AppParque.Api/Modules/Evaluaciones/Domain/Capacidades/VocabularioCapacidades.cs).
///
/// Se usan strings y no enums propios para no mantener dos copias del vocabulario. Los valores
/// válidos los declara <see cref="Features.EvaluacionAccesibilidad.CatalogoZonas"/>, que es la
/// única parte de la app que los escribe.
///
/// null en cualquier campo = el backend lo recibe como NoDeterminado, que no permite ni restringe:
/// obliga a preguntar. Es el comportamiento que queremos cuando el enfermero no llenó una zona.
/// </summary>
public record PerfilCapacidadesDto
{
    public LateralidadDto<PiernaDto> Piernas { get; init; } = new(new PiernaDto(), new PiernaDto());
    public LateralidadDto<ManoDto> Manos { get; init; } = new(new ManoDto(), new ManoDto());
    public MarchaDto Marcha { get; init; } = new();
    public PosturaDto Postura { get; init; } = new();
    public EmbarqueDto Embarque { get; init; } = new();
    public AntropometriaDto Cuerpo { get; init; } = new();
    public SensorialDto Sensorial { get; init; } = new();
    public SaludDto Salud { get; init; } = new();
    public ContextoDto Contexto { get; init; } = new();
}

public record LateralidadDto<T>(T Izquierda, T Derecha);

public record PiernaDto
{
    public string? Estado { get; init; }
    public ProtesisDto? Protesis { get; init; }
    public string? RodillaCompleta { get; init; }
    public string? PieFuncional { get; init; }
}

public record ProtesisDto(string Nivel, string Removible);

public record ManoDto
{
    public string? Estado { get; init; }
    public string? Agarre { get; init; }
    public string? Acortamiento { get; init; }
    public ProtesisDto? Protesis { get; init; }
}

public record MarchaDto
{
    public string? Capacidad { get; init; }
    public List<string> Apoyos { get; init; } = [];
    public string? Escaleras { get; init; }
}

public record PosturaDto
{
    public string? SedestacionErguida { get; init; }
    public string? FlexionPiernas { get; init; }
    public string? AcomodacionSegura { get; init; }
}

public record EmbarqueDto
{
    public string? Autonomo { get; init; }
    public string? SillaRuedas { get; init; }
}

public record AntropometriaDto
{
    public int? EstaturaCm { get; init; }
    public int? PesoKg { get; init; }
    public int? EdadAnios { get; init; }
}

public record SensorialDto
{
    public string? Vision { get; init; }
    public string? Audicion { get; init; }
    public string? Cognitivo { get; init; }
    public string? ComprendeInstrucciones { get; init; }
}

public record SaludDto
{
    public string? Embarazo { get; init; }
    public int? SemanasEmbarazo { get; init; }
    public string? Cardiaco { get; init; }
    public string? Osteomuscular { get; init; }
    public string? CirugiaReciente { get; init; }
    public string? BajoCuidadoMedico { get; init; }
    public string? Convulsiones { get; init; }
    public string? TrastornoMental { get; init; }
    public string? MareoVertigo { get; init; }
    public string? MiedoAlturas { get; init; }
    public string? Claustrofobia { get; init; }
}

public record ContextoDto
{
    public string? Rol { get; init; }
    public AcompananteDto? Acompanante { get; init; }
}

public record AcompananteDto
{
    public bool Presente { get; init; }
    public bool EsAdulto { get; init; }
    public int? EstaturaCm { get; init; }
    public string? Embarazo { get; init; }
    public string? PuedeAsistirEmbarque { get; init; }
}

/// <summary>Cuerpo de POST /api/diagnostico. Atracciones vacío = las 29 del catálogo.</summary>
public record DiagnosticoRequestDto(PerfilCapacidadesDto Perfil, List<string>? Atracciones = null);
