namespace AppParque.Shared.Models;

public record ResultadoAtraccionGuardadoDto(
    string CodigoAtraccion,
    string NombreAtraccion,
    string Veredicto,
    bool? AjusteManual,
    string? Comentario);

/// <summary>Cuerpo de POST /api/historial — lo que crea el botón "Confirmar y guardar en
/// historial" de DictamenView, uniendo el perfil del test, el dictamen ya calculado, la condición
/// de texto libre y la respuesta de la IA si se preguntó.</summary>
public record CrearDiagnosticoGuardadoRequestDto(
    int VisitanteId,
    PerfilCapacidadesDto Perfil,
    string? CondicionAdicional,
    List<ResultadoAtraccionGuardadoDto> Resultados,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    string? RespuestaIATexto,
    string? RespuestaIAModelo,
    Guid? RespuestaIASesionId);

/// <summary>Fila liviana del historial — una por diagnóstico confirmado.</summary>
public record HistorialDiagnosticoItemDto(
    int Id,
    int VisitanteId,
    string VisitanteNombre,
    string VisitanteNumeroDocumento,
    int EnfermeroId,
    string Enfermero,
    DateTime Fecha,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    bool TuvoCondicionAdicional,
    bool TuvoRespuestaIA);

/// <summary>Un mensaje de la conversación de IA que produjo RespuestaIATexto — ver MensajeSesion en la API.</summary>
public record MensajeIaDto(string Rol, string Contenido, DateTime CreadoEn);

/// <summary>Respuesta de GET /api/historial/{id} — el detalle completo para la pantalla "Ver detalles".</summary>
public record HistorialDetalleDto(
    int Id,
    int VisitanteId,
    string VisitanteNombre,
    string VisitanteNumeroDocumento,
    int EnfermeroId,
    string Enfermero,
    DateTime Fecha,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    string? CondicionAdicional,
    string PerfilJson,
    List<ResultadoAtraccionGuardadoDto> Resultados,
    string? RespuestaIATexto,
    string? RespuestaIAModelo,
    List<MensajeIaDto> ConsultasIA);
