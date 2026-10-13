namespace AppParque.Api.Modules.Evaluaciones.Api;

public record ResultadoAtraccionGuardadoRequest(
    string CodigoAtraccion,
    string NombreAtraccion,
    string Veredicto,
    bool? AjusteManual,
    string? Comentario);

/// <param name="Perfil">El mismo <c>PerfilCapacidades</c> que ya viaja en <c>DiagnosticoRequest</c>
/// — se recibe tal cual como JSON y se guarda sin reinterpretar, así una dimensión nueva del
/// vocabulario no exige tocar este contrato.</param>
public record CrearDiagnosticoGuardadoRequest(
    int VisitanteId,
    System.Text.Json.JsonElement Perfil,
    string? CondicionAdicional,
    List<ResultadoAtraccionGuardadoRequest> Resultados,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    string? RespuestaIATexto,
    string? RespuestaIAModelo,
    Guid? RespuestaIASesionId);

/// <summary>Fila liviana para la lista del historial — sin el detalle de atracciones, igual
/// criterio que el <c>HistorialItemResponse</c> del motor viejo.</summary>
public record HistorialDiagnosticoItemResponse(
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

public record ResultadoAtraccionGuardadoResponse(
    string CodigoAtraccion,
    string NombreAtraccion,
    string Veredicto,
    bool? AjusteManual,
    string? Comentario);

/// <summary>Un mensaje de la sesión de chat que produjo <see cref="HistorialDetalleResponse.RespuestaIATexto"/>
/// — "user" | "assistant", ver MensajeSesion.</summary>
public record MensajeIaResponse(string Rol, string Contenido, DateTime CreadoEn);

/// <summary>El detalle completo de un diagnóstico guardado, para la pantalla "Ver detalles" del
/// historial: respuestas del perfil, veredicto por atracción y la conversación de IA completa de
/// ese caso (no solo la última respuesta, que es lo único que guarda DiagnosticoGuardado).</summary>
public record HistorialDetalleResponse(
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
    List<ResultadoAtraccionGuardadoResponse> Resultados,
    string? RespuestaIATexto,
    string? RespuestaIAModelo,
    List<MensajeIaResponse> ConsultasIA);
