using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Api;

public record GrupoRequest(string Codigo, string Nombre, string? Descripcion);

public record GrupoResponse(int Id, string Codigo, string Nombre, string? Descripcion)
{
    public static GrupoResponse From(Grupo g) => new(g.Id, g.Codigo, g.Nombre, g.Descripcion);
}

public record CondicionRequest(string Codigo, string Nombre, string? Descripcion);

public record CondicionResponse(int Id, string Codigo, string Nombre, string? Descripcion)
{
    public static CondicionResponse From(Condicion c) => new(c.Id, c.Codigo, c.Nombre, c.Descripcion);
}
