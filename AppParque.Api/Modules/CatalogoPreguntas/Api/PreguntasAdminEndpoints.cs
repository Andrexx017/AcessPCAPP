using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoPreguntas.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoPreguntas.Api;

public static class PreguntasAdminEndpoints
{
    public static void MapPreguntasAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/preguntas").WithTags("Admin: Preguntas").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Preguntas.Include(p => p.Opciones).OrderBy(p => p.Orden)
                .Select(p => PreguntaAdminResponse.From(p)).ToListAsync()));

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var pregunta = await db.Preguntas.Include(p => p.Opciones).SingleOrDefaultAsync(p => p.Id == id);
            return pregunta is null ? Results.NotFound() : Results.Ok(PreguntaAdminResponse.From(pregunta));
        });

        group.MapPost("/", async (CrearPreguntaRequest request, AppDbContext db) =>
        {
            if (!Enum.TryParse<TipoPregunta>(request.Tipo, out var tipo))
                return Results.BadRequest($"Tipo de pregunta inválido: '{request.Tipo}'. Valores válidos: {string.Join(", ", Enum.GetNames<TipoPregunta>())}.");

            var pregunta = new Pregunta { Texto = request.Texto, Tipo = tipo, Orden = request.Orden, Activa = request.Activa };

            foreach (var opcion in request.Opciones)
                pregunta.Opciones.Add(new OpcionRespuesta { Codigo = opcion.Codigo, Texto = opcion.Texto, Orden = opcion.Orden });

            db.Preguntas.Add(pregunta);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/preguntas/{pregunta.Id}", PreguntaAdminResponse.From(pregunta)),
                "Uno de los códigos de opción ya existe.");
        });

        group.MapPut("/{id:int}", async (int id, PreguntaAdminRequest request, AppDbContext db) =>
        {
            if (!Enum.TryParse<TipoPregunta>(request.Tipo, out var tipo))
                return Results.BadRequest($"Tipo de pregunta inválido: '{request.Tipo}'. Valores válidos: {string.Join(", ", Enum.GetNames<TipoPregunta>())}.");

            var pregunta = await db.Preguntas.Include(p => p.Opciones).SingleOrDefaultAsync(p => p.Id == id);
            if (pregunta is null)
                return Results.NotFound();

            pregunta.Texto = request.Texto;
            pregunta.Tipo = tipo;
            pregunta.Orden = request.Orden;
            pregunta.Activa = request.Activa;

            await db.SaveChangesAsync();
            return Results.Ok(PreguntaAdminResponse.From(pregunta));
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var pregunta = await db.Preguntas.FindAsync(id);
            if (pregunta is null)
                return Results.NotFound();

            db.Preguntas.Remove(pregunta);

            return await DbConflictHelper.TryDeleteAsync(
                db,
                "No se puede eliminar: una de sus opciones ya está usada en evaluaciones históricas. Desactívala en vez de eliminarla.");
        });

        group.MapPost("/{id:int}/opciones", async (int id, OpcionRespuestaAdminRequest request, AppDbContext db) =>
        {
            var pregunta = await db.Preguntas.Include(p => p.Opciones).SingleOrDefaultAsync(p => p.Id == id);
            if (pregunta is null)
                return Results.NotFound();

            var opcion = new OpcionRespuesta { Codigo = request.Codigo, Texto = request.Texto, Orden = request.Orden };
            pregunta.Opciones.Add(opcion);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/preguntas/{id}/opciones/{opcion.Id}", OpcionRespuestaAdminResponse.From(opcion)),
                "Ya existe una opción con ese código.");
        });

        group.MapPut("/{id:int}/opciones/{opcionId:int}", async (int id, int opcionId, OpcionRespuestaAdminRequest request, AppDbContext db) =>
        {
            var opcion = await db.OpcionesRespuesta.SingleOrDefaultAsync(o => o.Id == opcionId && o.PreguntaId == id);
            if (opcion is null)
                return Results.NotFound();

            opcion.Codigo = request.Codigo;
            opcion.Texto = request.Texto;
            opcion.Orden = request.Orden;

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Ok(OpcionRespuestaAdminResponse.From(opcion)),
                "Ya existe una opción con ese código.");
        });

        group.MapDelete("/{id:int}/opciones/{opcionId:int}", async (int id, int opcionId, AppDbContext db) =>
        {
            var opcion = await db.OpcionesRespuesta.SingleOrDefaultAsync(o => o.Id == opcionId && o.PreguntaId == id);
            if (opcion is null)
                return Results.NotFound();

            db.OpcionesRespuesta.Remove(opcion);

            return await DbConflictHelper.TryDeleteAsync(
                db,
                "No se puede eliminar: la opción ya está usada en evaluaciones históricas.");
        });
    }
}
