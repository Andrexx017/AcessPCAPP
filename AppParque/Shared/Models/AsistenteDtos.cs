namespace AppParque.Shared.Models;

public record ProveedorIADto(
    int Id,
    string Nombre,
    string BaseUrl,
    string Modelo,
    int Prioridad,
    bool Activo,
    bool SoportaToolCalling,
    bool SoportaVision,
    bool TieneKey,
    string? UltimoEstado,
    DateTime? UltimoUsoEn);

/// <param name="ApiKey">Null/vacío al editar = conserva la key que ya tenía guardada.</param>
public record ProveedorIARequestDto(
    string Nombre,
    string BaseUrl,
    string Modelo,
    string? ApiKey,
    int Prioridad,
    bool Activo,
    bool SoportaToolCalling,
    bool SoportaVision);

public record DocumentoContextoDto(int Id, string Nombre, int LongitudContenido, DateTime CargadoEn, bool Activo);

public record DocumentoContextoRequestDto(string Nombre, string Contenido, bool Activo);

public record PromptIADto(string Clave, string Titulo, string Contenido, DateTime ActualizadoEn);

public record PromptIARequestDto(string Contenido);

/// <param name="SesionId">Null en el primer mensaje; reenviar el que vino en la respuesta anterior
/// para que el servidor mantenga la memoria de los últimos 5 minutos.</param>
/// <param name="ImagenBase64">Sin el prefijo data:. Solo se procesa si el proveedor activo soporta
/// visión — si no, el bot avisa en vez de ignorarla.</param>
/// <param name="Modo">El tema elegido en el menú previo al chat (ver ModosAsistente). Null solo
/// debería pasar si algún día se permite escribir sin elegir tema.</param>
public record ChatRequestDto(string Mensaje, Guid? SesionId, string? ImagenBase64, string? Modo = null);

public record ChatResponseDto(string Respuesta, string ModeloUsado, Guid SesionId);
