namespace AppParque.Api.Modules.Asistente.Domain;

/// <summary>
/// Un modelo configurado para el chat de asistencia, con su propia API key. La cadena de
/// fallback (RF del módulo "Asistente IA") recorre los proveedores <see cref="Activo"/> en orden
/// de <see cref="Prioridad"/> (menor primero) y pasa al siguiente si el actual responde con límite
/// de cuota alcanzado — así se pueden encadenar modelos gratuitos antes de caer en uno de pago,
/// sin tocar código: todo se administra desde la pantalla de admin.
///
/// Todos los proveedores soportados (Groq, Gemini, OpenAI) exponen una API compatible con el
/// formato de chat completions de OpenAI, así que un solo cliente HTTP sirve para todos — lo único
/// que cambia es <see cref="BaseUrl"/>, <see cref="Modelo"/> y la key.
/// </summary>
public class ProveedorIA
{
    public int Id { get; set; }

    /// <summary>Nombre para mostrar en el admin, ej. "Groq · Llama 3.3 70B".</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Base de la API, ej. "https://api.groq.com/openai/v1".</summary>
    public string BaseUrl { get; set; } = null!;

    /// <summary>Identificador del modelo tal como lo espera el proveedor, ej. "llama-3.3-70b-versatile".</summary>
    public string Modelo { get; set; } = null!;

    /// <summary>Cifrada con Data Protection antes de guardar (ver ProveedorIACliente). Nunca se
    /// devuelve en texto plano por la API después de guardada.</summary>
    public string ApiKeyCifrada { get; set; } = null!;

    /// <summary>Orden de intento en la cadena de fallback. Menor = se intenta primero.</summary>
    public int Prioridad { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>
    /// Si el modelo soporta "function calling" de forma confiable. El diseño completo depende de
    /// que el modelo LLAME a evaluar_atraccion en vez de inventar la respuesta — un modelo sin
    /// tool calling confiable es peor que no tenerlo en la cadena, así que se marca a mano (no se
    /// puede deducir del proveedor: varía por modelo específico) y el fallback lo salta si es false.
    /// </summary>
    public bool SoportaToolCalling { get; set; } = true;

    /// <summary>Si el modelo acepta imágenes en el mensaje (ej. gpt-4o, Gemini). Groq gpt-oss y
    /// o3-mini NO la soportan — si el visitante adjunta una imagen y el proveedor activo tiene esto
    /// en false, se le avisa al personal en vez de mandarla y que el proveedor falle o la ignore.</summary>
    public bool SoportaVision { get; set; }

    /// <summary>"ok" | "limite_alcanzado" | "error" | null (nunca probado). Informativo para el admin.</summary>
    public string? UltimoEstado { get; set; }

    public DateTime? UltimoUsoEn { get; set; }
}
