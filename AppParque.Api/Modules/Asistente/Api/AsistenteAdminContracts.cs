using AppParque.Api.Modules.Asistente.Domain;

namespace AppParque.Api.Modules.Asistente.Api;

public record ProveedorIAResponse(
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
    DateTime? UltimoUsoEn)
{
    public static ProveedorIAResponse From(ProveedorIA p) => new(
        p.Id, p.Nombre, p.BaseUrl, p.Modelo, p.Prioridad, p.Activo,
        p.SoportaToolCalling, p.SoportaVision, !string.IsNullOrEmpty(p.ApiKeyCifrada), p.UltimoEstado, p.UltimoUsoEn);
}

/// <param name="ApiKey">Obligatoria al crear. Al editar: vacío o ausente = conserva la key actual
/// (nunca se devuelve en texto plano, así que no hay forma de "no cambiarla" salvo dejarla así).</param>
public record ProveedorIARequest(
    string Nombre,
    string BaseUrl,
    string Modelo,
    string? ApiKey,
    int Prioridad,
    bool Activo,
    bool SoportaToolCalling,
    bool SoportaVision);

public record DocumentoContextoResponse(int Id, string Nombre, int LongitudContenido, DateTime CargadoEn, bool Activo)
{
    public static DocumentoContextoResponse From(DocumentoContexto d) => new(d.Id, d.Nombre, d.Contenido.Length, d.CargadoEn, d.Activo);
}

public record DocumentoContextoRequest(string Nombre, string Contenido, bool Activo);

public record PromptIAResponse(string Clave, string Titulo, string Contenido, DateTime ActualizadoEn)
{
    public static PromptIAResponse From(PromptIA p) => new(p.Clave, p.Titulo, p.Contenido, p.ActualizadoEn);
}

public record PromptIARequest(string Contenido);

/// <summary>Agregado para el dashboard de inicio: cuántas preguntas (mensajes con rol "user") se le
/// hicieron al asistente hoy (UTC) — cada una es una "consulta" sin importar la sesión.</summary>
public record ConsultasHoyResponse(int Total);
