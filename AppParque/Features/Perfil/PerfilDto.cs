namespace AppParque.Features.Perfil;

public record PerfilDto(int Id, string NombreCompleto, string Username, string? Email, string? FotoBase64, string Rol);
