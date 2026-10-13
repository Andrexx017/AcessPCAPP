namespace AppParque.Api.Modules.Asistente.Api;

/// <param name="Mensaje">El mensaje nuevo del personal. El historial ya NO lo manda el cliente —
/// lo guarda el servidor por sesión (ver SesionId) para que la "memoria" de 5 minutos funcione
/// aunque se cierre y reabra la pantalla de chat.</param>
/// <param name="SesionId">Null en el primer mensaje. El cliente debe reenviar el que vino en la
/// respuesta anterior para seguir la misma conversación; si pasaron más de 5 minutos sin mandar
/// nada, el servidor arranca una sesión nueva igual, así que no hace falta que el cliente lo sepa.</param>
/// <param name="ImagenBase64">Una imagen adjunta (ej. un permiso médico), ya en base64 sin el
/// prefijo data:. Solo se procesa si el proveedor activo tiene SoportaVision — si no, el bot avisa
/// que no la pudo leer en vez de ignorarla en silencio.</param>
/// <param name="Modo">El tema que el personal eligió en el menú previo al chat (uno de
/// ClavesPrompt — "evaluacion", "consulta_atracciones", "horarios", "contexto_app", "otros").
/// Null = sin tema (compatibilidad hacia atrás).</param>
public record ChatRequest(string Mensaje, Guid? SesionId, string? ImagenBase64, string? Modo = null);

public record ChatResponse(string Respuesta, string ModeloUsado, Guid SesionId);
