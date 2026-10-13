namespace AppParque.Api.Modules.Asistente.Domain;

/// <summary>
/// Un mensaje de una sesión de chat. La "memoria" del bot: mientras la última entrada de una
/// sesión tenga menos de 5 minutos, las preguntas siguientes del mismo visitante de sesión ven el
/// historial. Pasados los 5 minutos sin actividad, la sesión se considera vencida y la siguiente
/// pregunta arranca una nueva — no hay borrado explícito, el filtro es por tiempo (ver
/// AsistenteOrquestador.VentanaSesion).
///
/// Solo se guarda el texto visible de la conversación (rol "user"/"assistant"), no las llamadas a
/// herramientas intermedias: esas se resuelven de nuevo en cada turno y no son "memoria" desde la
/// perspectiva del personal.
/// </summary>
public class MensajeSesion
{
    public int Id { get; set; }

    public Guid SesionId { get; set; }

    public int UsuarioId { get; set; }

    /// <summary>"user" | "assistant".</summary>
    public string Rol { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
