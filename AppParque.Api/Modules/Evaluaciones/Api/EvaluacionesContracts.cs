namespace AppParque.Api.Modules.Evaluaciones.Api;

public record CrearEvaluacionRequest(int VisitanteId, int? Edad, int? Estatura, List<int> OpcionesRespuestaIds);

public record AjustarPreseleccionRequest(bool ValidadaPersonal, string? Comentario);

public record AtraccionResultadoResponse(
    int AtraccionId,
    string AtraccionNombre,
    bool PreseleccionadaAutomatica,
    bool? ValidadaPersonal,
    string? Comentario);

public record EvaluacionResponse(
    int Id,
    int VisitanteId,
    int UsuarioId,
    DateTime Fecha,
    int? Edad,
    int? Estatura,
    List<string> GruposActivados,
    List<string> CondicionesActivadas,
    List<AtraccionResultadoResponse> Atracciones);
