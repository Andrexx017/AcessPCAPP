using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AppParque.Api.Modules.Asistente.Application;

/// <summary>
/// Groq, Gemini y OpenAI exponen (o emulan) el mismo formato de "chat completions" de OpenAI —
/// estas son las formas mínimas que necesitamos de ese contrato. Un solo cliente HTTP sirve para
/// los tres proveedores: lo único que cambia es la BaseUrl, el modelo y la key
/// (ver <see cref="AsistenteOrquestador"/>).
/// </summary>
public class OpenAiMensaje
{
    [JsonPropertyName("role")] public string Role { get; set; } = "";

    /// <summary>String para un mensaje de solo texto, o un array de "content parts" (texto +
    /// image_url) cuando hay una imagen adjunta — el mismo campo acepta las dos formas en el
    /// formato de OpenAI, por eso es JsonNode y no string. Usar <see cref="TextoPlano"/> para leer
    /// el texto sin importar cuál de las dos formas trae.</summary>
    [JsonPropertyName("content")] public JsonNode? Content { get; set; }

    [JsonPropertyName("tool_calls")] public List<OpenAiToolCall>? ToolCalls { get; set; }
    [JsonPropertyName("tool_call_id")] public string? ToolCallId { get; set; }

    public OpenAiMensaje() { }

    public OpenAiMensaje(string role, string? content)
    {
        Role = role;
        Content = content is null ? null : JsonValue.Create(content);
    }

    /// <summary>Mensaje de usuario con una imagen adjunta (data URI base64) además del texto.</summary>
    public static OpenAiMensaje ConImagen(string role, string texto, string imagenBase64, string mimeType = "image/jpeg") => new()
    {
        Role = role,
        Content = new JsonArray(
            new JsonObject { ["type"] = "text", ["text"] = texto },
            new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = $"data:{mimeType};base64,{imagenBase64}" } }),
    };

    /// <summary>El texto del mensaje sin importar si Content es un string plano o un array de
    /// content parts (en ese caso, concatena las partes de tipo "text").</summary>
    public string TextoPlano()
    {
        if (Content is null)
            return "";
        if (Content is JsonValue valor && valor.TryGetValue<string>(out var texto))
            return texto;
        if (Content is JsonArray partes)
            return string.Join(" ", partes
                .OfType<JsonObject>()
                .Where(p => p["type"]?.GetValue<string>() == "text")
                .Select(p => p["text"]?.GetValue<string>() ?? ""));
        return "";
    }
}

public class OpenAiToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiFunctionCall Function { get; set; } = new();
}

public class OpenAiFunctionCall
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("arguments")] public string Arguments { get; set; } = "";
}

public class OpenAiTool
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OpenAiFunctionDef Function { get; set; } = new();
}

public class OpenAiFunctionDef
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("parameters")] public JsonObject Parameters { get; set; } = new();
}

public class OpenAiChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("messages")] public List<OpenAiMensaje> Messages { get; set; } = [];
    [JsonPropertyName("tools")] public List<OpenAiTool>? Tools { get; set; }
    [JsonPropertyName("tool_choice")] public string? ToolChoice { get; set; }

    /// <summary>Null = no se envía. Los modelos de razonamiento (o3-mini y similares) rechazan la
    /// llamada entera si reciben "temperature" — no vale la pena fijar un valor por defecto acá
    /// cuando el costo de equivocarse es romper el proveedor completo.</summary>
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
}

public class OpenAiChatResponse
{
    [JsonPropertyName("choices")] public List<OpenAiChoice> Choices { get; set; } = [];
    [JsonPropertyName("error")] public JsonObject? Error { get; set; }
}

public class OpenAiChoice
{
    [JsonPropertyName("message")] public OpenAiMensaje Message { get; set; } = new();
    [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
}

/// <summary>Distingue "este proveedor se quedó sin cupo, prueba el siguiente de la cadena" de
/// cualquier otro error real (key inválida, proveedor caído, etc.).</summary>
public class LimiteAlcanzadoException(string proveedor) : Exception($"{proveedor} alcanzó su límite de uso.");
