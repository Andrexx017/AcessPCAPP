using System.Security.Claims;
using AppParque.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Identidad.Api;

public static class PerfilEndpoints
{
    public static void MapPerfilEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/perfil").WithTags("Perfil").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal caller, AppDbContext db) =>
        {
            var usuario = await BuscarUsuarioActualAsync(caller, db);
            return usuario is null ? Results.NotFound() : Results.Ok(PerfilResponse.From(usuario));
        });

        group.MapPut("/", async (ActualizarPerfilRequest request, ClaimsPrincipal caller, AppDbContext db) =>
        {
            var usuario = await BuscarUsuarioActualAsync(caller, db);
            if (usuario is null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.NombreCompleto))
                return Results.BadRequest("El nombre completo es obligatorio.");

            usuario.NombreCompleto = request.NombreCompleto;
            usuario.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(PerfilResponse.From(usuario));
        });

        group.MapPut("/foto", async (ActualizarFotoPerfilRequest request, ClaimsPrincipal caller, AppDbContext db) =>
        {
            var usuario = await BuscarUsuarioActualAsync(caller, db);
            if (usuario is null)
                return Results.NotFound();

            usuario.FotoBase64 = string.IsNullOrWhiteSpace(request.FotoBase64) ? null : request.FotoBase64;

            await db.SaveChangesAsync();
            return Results.Ok(PerfilResponse.From(usuario));
        });
    }

    private static Task<Domain.Usuario?> BuscarUsuarioActualAsync(ClaimsPrincipal caller, AppDbContext db)
    {
        var idTexto = caller.FindFirstValue(ClaimTypes.NameIdentifier) ?? caller.FindFirstValue("sub");
        return int.TryParse(idTexto, out var id)
            ? db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
            : Task.FromResult<Domain.Usuario?>(null);
    }
}
