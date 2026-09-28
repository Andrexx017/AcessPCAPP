namespace AppParque.Features.Administracion.AdminUsuarios;

public record UsuarioAdminDto(int Id, string NombreCompleto, string Username, string Rol, bool Activo, DateTime CreatedAt);
