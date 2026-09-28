namespace AppParque.Features.Auth;

public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiraEn, string NombreCompleto, string Rol);
