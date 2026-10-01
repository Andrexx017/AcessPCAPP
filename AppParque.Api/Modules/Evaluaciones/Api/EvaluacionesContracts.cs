namespace AppParque.Api.Modules.Evaluaciones.Api;

public record CrearEvaluacionRequest(int VisitanteId, int? Edad, int? Estatura, List<int> OpcionesRespuestaIds);

public record AjustarPreseleccionRequest(bool ValidadaPersonal, string? Comentario);

/// <summary>RF-14: Edad/Estatura son opcionales — si no se envían, se reutilizan los de la evaluación
/// origen (por si cambiaron, ej. un niño que creció, se pueden actualizar sin repetir el test).</summary>
public record ReutilizarEvaluacionRequest(int? Edad, int? Estatura);

public record AtraccionResultadoResponse(
    int AtraccionId,
    string AtraccionNombre,
    string? AtraccionDescripcion,
    string? AtraccionImagenUrl,
    int? AtraccionAlturaMinima,
    int? AtraccionAlturaMaxima,
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

/// <summary>Código + nombre visible de un grupo/condición activado — el historial necesita el nombre
/// para mostrar filtros y etiquetas legibles sin duplicar el catálogo en la app.</summary>
public record RestriccionDto(string Codigo, string Nombre);

/// <summary>Fila resumida para el historial (RF-08) — sin el detalle de atracciones, para que la lista sea liviana.</summary>
public record HistorialItemResponse(
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
