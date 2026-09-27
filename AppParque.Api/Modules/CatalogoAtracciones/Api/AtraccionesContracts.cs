using AppParque.Api.Modules.CatalogoAtracciones.Domain;

namespace AppParque.Api.Modules.CatalogoAtracciones.Api;

public record AtraccionRequest(
    string Nombre,
    string? Descripcion,
    string? ImagenUrl,
    int? AlturaMinima,
    int? AlturaMaxima,
    bool Activa);

public record RestriccionResponse(int Id, string Codigo, string Nombre, bool Permitido);

public record AtraccionResponse(
    int Id,
    string Nombre,
    string? Descripcion,
    string? ImagenUrl,
    int? AlturaMinima,
    int? AlturaMaxima,
    bool Activa,
    List<RestriccionResponse> RestriccionesGrupo,
    List<RestriccionResponse> RestriccionesCondicion)
{
    public static AtraccionResponse From(Atraccion a) => new(
        a.Id,
        a.Nombre,
        a.Descripcion,
        a.ImagenUrl,
        a.AlturaMinima,
        a.AlturaMaxima,
        a.Activa,
        a.RestriccionesGrupo.Select(r => new RestriccionResponse(r.Grupo.Id, r.Grupo.Codigo, r.Grupo.Nombre, r.Permitido)).OrderBy(r => r.Codigo).ToList(),
        a.RestriccionesCondicion.Select(r => new RestriccionResponse(r.Condicion.Id, r.Condicion.Codigo, r.Condicion.Nombre, r.Permitido)).OrderBy(r => r.Codigo).ToList());
}

/// <summary>Actualización en bloque: cada clave es el Codigo del grupo/condición, el valor es el nuevo Permitido.
/// Solo toca las claves enviadas — crea la fila si no existía, la actualiza si ya existía.</summary>
public record ActualizarRestriccionesRequest(Dictionary<string, bool>? Grupos, Dictionary<string, bool>? Condiciones);
