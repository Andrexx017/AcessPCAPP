using AppParque.Api.Data;
using AppParque.Api.Modules.Visitantes.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Visitantes.Api;

public static class VisitantesEndpoints
{
    public static void MapVisitantesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/visitantes").WithTags("Visitantes").RequireAuthorization();

        group.MapGet("/buscar", BuscarAsync);
        group.MapPost("/", CrearAsync);
        group.MapGet("/{id:int}", ObtenerPorIdAsync);
        group.MapGet("/{id:int}/evaluaciones", ListarEvaluacionesAsync);
    }

    private static async Task<IResult> BuscarAsync(string tipoDocumento, string numeroDocumento, AppDbContext db)
    {
        var visitante = await db.Visitantes
            .SingleOrDefaultAsync(v => v.TipoDocumento == tipoDocumento && v.NumeroDocumento == numeroDocumento);

        return visitante is null ? Results.NotFound() : Results.Ok(VisitanteResponse.From(visitante));
    }

    private static async Task<IResult> CrearAsync(CrearVisitanteRequest request, AppDbContext db)
    {
        var yaExiste = await db.Visitantes.AnyAsync(v =>
            v.TipoDocumento == request.TipoDocumento && v.NumeroDocumento == request.NumeroDocumento);

        if (yaExiste)
            return Results.Conflict("Ya existe un visitante con ese tipo y número de documento.");

        var visitante = new Visitante
        {
            TipoDocumento = request.TipoDocumento,
            NumeroDocumento = request.NumeroDocumento,
            Nombre = request.Nombre,
        };

        db.Visitantes.Add(visitante);
        await db.SaveChangesAsync();

        return Results.Created($"/api/visitantes/{visitante.Id}", VisitanteResponse.From(visitante));
    }

    private static async Task<IResult> ObtenerPorIdAsync(int id, AppDbContext db)
    {
        var visitante = await db.Visitantes.FindAsync(id);
        return visitante is null ? Results.NotFound() : Results.Ok(VisitanteResponse.From(visitante));
    }

    private static async Task<IResult> ListarEvaluacionesAsync(int id, AppDbContext db)
    {
        var existe = await db.Visitantes.AnyAsync(v => v.Id == id);
        if (!existe)
            return Results.NotFound();

        var evaluaciones = await db.Evaluaciones
            .Where(e => e.VisitanteId == id)
            .OrderByDescending(e => e.Fecha)
            .Select(e => new
            {
                e.Id,
                e.Fecha,
                e.Edad,
                e.Estatura,
                Enfermero = e.Usuario.NombreCompleto,
            })
            .ToListAsync();

        return Results.Ok(evaluaciones);
    }
}
