using AppParque.Api.Modules.Identidad.Domain;

namespace AppParque.Api.Modules.Identidad.Api;

public record CrearUsuarioRequest(string NombreCompleto, string Username, string Password, string Rol);

public record ActualizarUsuarioRequest(string NombreCompleto, string Rol, bool Activo);

public record CambiarPasswordRequest(string NuevoPassword);

public record UsuarioResponse(int Id, string NombreCompleto, string Username, string Rol, bool Activo, DateTime CreatedAt)
{
    public static UsuarioResponse From(Usuario u) => new(u.Id, u.NombreCompleto, u.Username, u.Rol.ToString(), u.Activo, u.CreatedAt);
}
