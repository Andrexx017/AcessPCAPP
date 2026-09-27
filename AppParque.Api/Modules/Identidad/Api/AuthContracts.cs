using AppParque.Api.Modules.Identidad.Domain;

namespace AppParque.Api.Modules.Identidad.Api;

public record LoginRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiraEn, string NombreCompleto, string Rol)
{
    public static AuthResponse From(string accessToken, string rawRefreshToken, RefreshToken refreshTokenEntity, Usuario usuario) =>
        new(accessToken, rawRefreshToken, refreshTokenEntity.ExpiraEn, usuario.NombreCompleto, usuario.Rol.ToString());
}
