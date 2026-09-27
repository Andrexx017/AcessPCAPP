using System.Security.Claims;
using AppParque.Api.Data;
using AppParque.Api.Modules.Identidad.Application;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Identidad.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);

        // Endpoint de prueba: confirma que un access token válido llega con los claims esperados.
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            Id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
            Username = user.FindFirstValue(ClaimTypes.Name),
            Rol = user.FindFirstValue(ClaimTypes.Role),
        })).RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, AppDbContext db, ITokenService tokens)
    {
        var usuario = await db.Usuarios.SingleOrDefaultAsync(u => u.Username == request.Username && u.Activo);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
            return Results.Unauthorized();

        var accessToken = tokens.CreateAccessToken(usuario);
        var (rawRefreshToken, refreshTokenEntity) = tokens.CreateRefreshToken(usuario.Id);

        db.RefreshTokens.Add(refreshTokenEntity);
        await db.SaveChangesAsync();

        return Results.Ok(AuthResponse.From(accessToken, rawRefreshToken, refreshTokenEntity, usuario));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, AppDbContext db, ITokenService tokens)
    {
        var hash = tokens.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(r => r.Usuario)
            .SingleOrDefaultAsync(r => r.TokenHash == hash);

        if (existing is null)
            return Results.Unauthorized();

        if (existing.Revocado)
        {
            // Un refresh token ya rotado/revocado que vuelve a presentarse es señal de robo:
            // se revocan todos los tokens vigentes del usuario para forzar un nuevo login.
            await db.RefreshTokens
                .Where(r => r.UsuarioId == existing.UsuarioId && !r.Revocado)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Revocado, true)
                    .SetProperty(r => r.RevocadoEn, DateTime.UtcNow));

            return Results.Unauthorized();
        }

        if (existing.ExpiraEn < DateTime.UtcNow || !existing.Usuario.Activo)
            return Results.Unauthorized();

        existing.Revocado = true;
        existing.RevocadoEn = DateTime.UtcNow;

        var accessToken = tokens.CreateAccessToken(existing.Usuario);
        var (rawRefreshToken, newEntity) = tokens.CreateRefreshToken(existing.UsuarioId);
        db.RefreshTokens.Add(newEntity);

        await db.SaveChangesAsync();

        return Results.Ok(AuthResponse.From(accessToken, rawRefreshToken, newEntity, existing.Usuario));
    }

    private static async Task<IResult> LogoutAsync(RefreshRequest request, AppDbContext db, ITokenService tokens)
    {
        var hash = tokens.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(r => r.TokenHash == hash);

        if (existing is not null && !existing.Revocado)
        {
            existing.Revocado = true;
            existing.RevocadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return Results.NoContent();
    }
}
