namespace AppParque.Shared.Models;

// Usado por EvaluacionAccesibilidad (crea la evaluación) y ValidacionAtracciones (la muestra/ajusta),
// por eso vive en Shared/Models en vez de dentro de una sola feature.
public record AtraccionResultadoDto(
    int AtraccionId,
    string AtraccionNombre,
    string? AtraccionDescripcion,
    string? AtraccionImagenUrl,
    int? AtraccionAlturaMinima,
    int? AtraccionAlturaMaxima,
    bool PreseleccionadaAutomatica,
    bool? ValidadaPersonal,
    string? Comentario);

public record EvaluacionResponseDto(
    int Id,
    int VisitanteId,
    int UsuarioId,
    DateTime Fecha,
    int? Edad,
    int? Estatura,
    List<string> GruposActivados,
    List<string> CondicionesActivadas,
    List<AtraccionResultadoDto> Atracciones);
