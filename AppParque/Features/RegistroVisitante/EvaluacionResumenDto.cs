namespace AppParque.Features.RegistroVisitante;

/// <summary>Forma de cada item que devuelve GET /api/visitantes/{id}/evaluaciones — una fila liviana
/// por evaluación pasada, usada solo para mostrar "tu última evaluación" en ConfirmacionEvaluacionView.</summary>
public record EvaluacionResumenDto(int Id, DateTime Fecha, int? Edad, int? Estatura, string Enfermero);
