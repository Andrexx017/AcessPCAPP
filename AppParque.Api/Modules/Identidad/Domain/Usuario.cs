using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.Identidad.Domain;

public class Usuario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
