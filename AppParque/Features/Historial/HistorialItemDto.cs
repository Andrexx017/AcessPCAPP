namespace AppParque.Features.Historial;

public record RestriccionDto(string Codigo, string Nombre);

public record HistorialItemDto(
    int EvaluacionId,
    int VisitanteId,
    string VisitanteNombre,
    string VisitanteNumeroDocumento,
    int EnfermeroId,
    string Enfermero,
    DateTime Fecha,
    int? Estatura,
    List<RestriccionDto> GruposActivados,
    List<RestriccionDto> CondicionesActivadas);
