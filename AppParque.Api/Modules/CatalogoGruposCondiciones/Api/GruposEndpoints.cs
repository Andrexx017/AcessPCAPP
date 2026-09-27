using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Api;

public static class GruposEndpoints
{
    public static void MapGruposEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/grupos").WithTags("Admin: Grupos").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Grupos.OrderBy(g => g.Codigo).Select(g => GrupoResponse.From(g)).ToListAsync()));

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var grupo = await db.Grupos.FindAsync(id);
            return grupo is null ? Results.NotFound() : Results.Ok(GrupoResponse.From(grupo));
        });

        group.MapPost("/", async (GrupoRequest request, AppDbContext db) =>
        {
            var grupo = new Grupo { Codigo = request.Codigo, Nombre = request.Nombre, Descripcion = request.Descripcion };
            db.Grupos.Add(grupo);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/grupos/{grupo.Id}", GrupoResponse.From(grupo)),
                "Ya existe un grupo con ese código.");
        });

        group.MapPut("/{id:int}", async (int id, GrupoRequest request, AppDbContext db) =>
        {
            var grupo = await db.Grupos.FindAsync(id);
            if (grupo is null)
                return Results.NotFound();

            grupo.Codigo = request.Codigo;
            grupo.Nombre = request.Nombre;
            grupo.Descripcion = request.Descripcion;

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Ok(GrupoResponse.From(grupo)),
                "Ya existe un grupo con ese código.");
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var grupo = await db.Grupos.FindAsync(id);
            if (grupo is null)
                return Results.NotFound();

            db.Grupos.Remove(grupo);

            return await DbConflictHelper.TryDeleteAsync(
                db,
                "No se puede eliminar: el grupo ya está usado en evaluaciones históricas.");
        });
    }
}
