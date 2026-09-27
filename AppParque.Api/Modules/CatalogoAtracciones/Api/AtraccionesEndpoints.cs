using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoAtracciones.Api;

public static class AtraccionesEndpoints
{
    public static void MapAtraccionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/atracciones").WithTags("Admin: Atracciones").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await CargarConRestricciones(db).OrderBy(a => a.Nombre).Select(a => AtraccionResponse.From(a)).ToListAsync()));

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var atraccion = await CargarConRestricciones(db).SingleOrDefaultAsync(a => a.Id == id);
            return atraccion is null ? Results.NotFound() : Results.Ok(AtraccionResponse.From(atraccion));
        });

        group.MapPost("/", async (AtraccionRequest request, AppDbContext db) =>
        {
            var atraccion = MapearCampos(new Atraccion(), request);
            db.Atracciones.Add(atraccion);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/atracciones/{atraccion.Id}", AtraccionResponse.From(atraccion));
        });

        group.MapPut("/{id:int}", async (int id, AtraccionRequest request, AppDbContext db) =>
        {
            var atraccion = await db.Atracciones.FindAsync(id);
            if (atraccion is null)
                return Results.NotFound();

            MapearCampos(atraccion, request);
            await db.SaveChangesAsync();

            var actualizada = await CargarConRestricciones(db).SingleAsync(a => a.Id == id);
            return Results.Ok(AtraccionResponse.From(actualizada));
        });

        group.MapPut("/{id:int}/restricciones", async (int id, ActualizarRestriccionesRequest request, AppDbContext db) =>
        {
            var atraccion = await db.Atracciones
                .Include(a => a.RestriccionesGrupo)
                .Include(a => a.RestriccionesCondicion)
                .SingleOrDefaultAsync(a => a.Id == id);

            if (atraccion is null)
                return Results.NotFound();

            if (request.Grupos is { Count: > 0 })
            {
                var grupos = await db.Grupos.Where(g => request.Grupos.Keys.Contains(g.Codigo)).ToDictionaryAsync(g => g.Codigo);
                foreach (var (codigo, permitido) in request.Grupos)
                {
                    if (!grupos.TryGetValue(codigo, out var grupo))
                        return Results.BadRequest($"No existe un grupo con código '{codigo}'.");

                    var existente = atraccion.RestriccionesGrupo.SingleOrDefault(r => r.GrupoId == grupo.Id);
                    if (existente is null)
                        atraccion.RestriccionesGrupo.Add(new AtraccionGrupoRestriccion { GrupoId = grupo.Id, Permitido = permitido });
                    else
                        existente.Permitido = permitido;
                }
            }

            if (request.Condiciones is { Count: > 0 })
            {
                var condiciones = await db.Condiciones.Where(c => request.Condiciones.Keys.Contains(c.Codigo)).ToDictionaryAsync(c => c.Codigo);
                foreach (var (codigo, permitido) in request.Condiciones)
                {
                    if (!condiciones.TryGetValue(codigo, out var condicion))
                        return Results.BadRequest($"No existe una condición con código '{codigo}'.");

                    var existente = atraccion.RestriccionesCondicion.SingleOrDefault(r => r.CondicionId == condicion.Id);
                    if (existente is null)
                        atraccion.RestriccionesCondicion.Add(new AtraccionCondicionRestriccion { CondicionId = condicion.Id, Permitido = permitido });
                    else
                        existente.Permitido = permitido;
                }
            }

            await db.SaveChangesAsync();

            var actualizada = await CargarConRestricciones(db).SingleAsync(a => a.Id == id);
            return Results.Ok(AtraccionResponse.From(actualizada));
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var atraccion = await db.Atracciones.FindAsync(id);
            if (atraccion is null)
                return Results.NotFound();

            db.Atracciones.Remove(atraccion);

            return await DbConflictHelper.TryDeleteAsync(
                db,
                "No se puede eliminar: la atracción ya está usada en evaluaciones históricas. Desactívala en vez de eliminarla.");
        });
    }

    private static IQueryable<Atraccion> CargarConRestricciones(AppDbContext db) =>
        db.Atracciones
            .Include(a => a.RestriccionesGrupo).ThenInclude(r => r.Grupo)
            .Include(a => a.RestriccionesCondicion).ThenInclude(r => r.Condicion);

    private static Atraccion MapearCampos(Atraccion atraccion, AtraccionRequest request)
    {
        atraccion.Nombre = request.Nombre;
        atraccion.Descripcion = request.Descripcion;
        atraccion.ImagenUrl = request.ImagenUrl;
        atraccion.AlturaMinima = request.AlturaMinima;
        atraccion.AlturaMaxima = request.AlturaMaxima;
        atraccion.Activa = request.Activa;
        return atraccion;
    }
}
