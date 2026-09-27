using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Data;

/// <summary>
/// Los catálogos (Grupo, Condicion, Atraccion, OpcionRespuesta) están referenciados con
/// DeleteBehavior.Restrict desde las tablas de Evaluacion — a propósito, para no poder borrar
/// algo que ya forma parte de un historial. Esto centraliza el "traducir esa restricción de FK
/// a un 409 legible" en vez de dejar que se propague como 500.
/// </summary>
public static class DbConflictHelper
{
    public static async Task<IResult> TryDeleteAsync(AppDbContext db, string conflictMessage)
    {
        try
        {
            await db.SaveChangesAsync();
            return Results.NoContent();
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(conflictMessage);
        }
    }

    public static async Task<IResult> TrySaveAsync(AppDbContext db, Func<IResult> onSuccess, string conflictMessage)
    {
        try
        {
            await db.SaveChangesAsync();
            return onSuccess();
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(conflictMessage);
        }
    }
}
