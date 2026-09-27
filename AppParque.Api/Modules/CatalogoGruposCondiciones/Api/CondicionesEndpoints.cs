using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Api;

public static class CondicionesEndpoints
{
    public static void MapCondicionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/condiciones").WithTags("Admin: Condiciones").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Condiciones.OrderBy(c => c.Codigo).Select(c => CondicionResponse.From(c)).ToListAsync()));

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var condicion = await db.Condiciones.FindAsync(id);
            return condicion is null ? Results.NotFound() : Results.Ok(CondicionResponse.From(condicion));
        });

        group.MapPost("/", async (CondicionRequest request, AppDbContext db) =>
        {
            var condicion = new Condicion { Codigo = request.Codigo, Nombre = request.Nombre, Descripcion = request.Descripcion };
            db.Condiciones.Add(condicion);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/condiciones/{condicion.Id}", CondicionResponse.From(condicion)),
                "Ya existe una condición con ese código.");
        });

        group.MapPut("/{id:int}", async (int id, CondicionRequest request, AppDbContext db) =>
        {
            var condicion = await db.Condiciones.FindAsync(id);
            if (condicion is null)
                return Results.NotFound();

            condicion.Codigo = request.Codigo;
            condicion.Nombre = request.Nombre;
            condicion.Descripcion = request.Descripcion;

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Ok(CondicionResponse.From(condicion)),
                "Ya existe una condición con ese código.");
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var condicion = await db.Condiciones.FindAsync(id);
            if (condicion is null)
                return Results.NotFound();

            db.Condiciones.Remove(condicion);

            return await DbConflictHelper.TryDeleteAsync(
                db,
                "No se puede eliminar: la condición ya está usada en evaluaciones históricas.");
        });
    }
}
