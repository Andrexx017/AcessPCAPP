using System.Security.Claims;
using AppParque.Api.Data;
using AppParque.Api.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Identidad.Api;

public static class UsuariosAdminEndpoints
{
    public static void MapUsuariosAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/usuarios").WithTags("Admin: Usuarios").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
            Results.Ok(await db.Usuarios.OrderBy(u => u.Username).Select(u => UsuarioResponse.From(u)).ToListAsync()));

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var usuario = await db.Usuarios.FindAsync(id);
            return usuario is null ? Results.NotFound() : Results.Ok(UsuarioResponse.From(usuario));
        });

        group.MapPost("/", async (CrearUsuarioRequest request, AppDbContext db) =>
        {
            if (!Enum.TryParse<RolUsuario>(request.Rol, out var rol))
                return Results.BadRequest($"Rol inválido: '{request.Rol}'. Valores válidos: {string.Join(", ", Enum.GetNames<RolUsuario>())}.");

            if (await db.Usuarios.AnyAsync(u => u.Username == request.Username))
                return Results.Conflict("Ya existe un usuario con ese username.");

            var usuario = new Usuario
            {
                NombreCompleto = request.NombreCompleto,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = rol,
                Activo = true,
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/usuarios/{usuario.Id}", UsuarioResponse.From(usuario));
        });

        group.MapPut("/{id:int}", async (int id, ActualizarUsuarioRequest request, AppDbContext db, ClaimsPrincipal caller) =>
        {
            if (!Enum.TryParse<RolUsuario>(request.Rol, out var rol))
                return Results.BadRequest($"Rol inválido: '{request.Rol}'. Valores válidos: {string.Join(", ", Enum.GetNames<RolUsuario>())}.");

            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario is null)
                return Results.NotFound();

            var callerIdTexto = caller.FindFirstValue(ClaimTypes.NameIdentifier) ?? caller.FindFirstValue("sub");
            if (!request.Activo && callerIdTexto == usuario.Id.ToString())
                return Results.BadRequest("No puedes desactivar tu propio usuario.");

            usuario.NombreCompleto = request.NombreCompleto;
            usuario.Rol = rol;
            usuario.Activo = request.Activo;

            await db.SaveChangesAsync();
            return Results.Ok(UsuarioResponse.From(usuario));
        });

        group.MapPut("/{id:int}/password", async (int id, CambiarPasswordRequest request, AppDbContext db) =>
        {
            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario is null)
                return Results.NotFound();

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevoPassword);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
