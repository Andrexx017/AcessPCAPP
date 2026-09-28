using AppParque.Api.Modules.Identidad.Domain;

namespace AppParque.Api.Modules.Identidad.Api;

public record ActualizarPerfilRequest(string NombreCompleto, string? Email);

public record ActualizarFotoPerfilRequest(string FotoBase64);

public record PerfilResponse(int Id, string NombreCompleto, string Username, string? Email, string? FotoBase64, string Rol)
{
    public static PerfilResponse From(Usuario u) => new(u.Id, u.NombreCompleto, u.Username, u.Email, u.FotoBase64, u.Rol.ToString());
}
