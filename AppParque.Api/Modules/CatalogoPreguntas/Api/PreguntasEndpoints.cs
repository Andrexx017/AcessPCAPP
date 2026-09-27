using AppParque.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoPreguntas.Api;

public static class PreguntasEndpoints
{
    public static void MapPreguntasEndpoints(this IEndpointRouteBuilder app)
    {
        // No se expone OpcionRespuesta.Codigo: es un detalle interno del motor de reglas,
        // el cliente solo necesita el Id de la opción para enviarlo de vuelta al crear la evaluación.
        app.MapGet("/api/preguntas", async (AppDbContext db) =>
        {
            var preguntas = await db.Preguntas
                .Where(p => p.Activa)
                .OrderBy(p => p.Orden)
                .Select(p => new
                {
                    p.Id,
                    p.Texto,
                    p.Orden,
                    Tipo = p.Tipo.ToString(),
                    Opciones = p.Opciones
                        .OrderBy(o => o.Orden)
                        .Select(o => new { o.Id, o.Texto, o.Orden }),
                })
                .ToListAsync();

            return Results.Ok(preguntas);
        }).WithTags("Preguntas").RequireAuthorization();
    }
}
