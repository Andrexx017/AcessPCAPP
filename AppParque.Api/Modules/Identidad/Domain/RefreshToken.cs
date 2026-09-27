namespace AppParque.Api.Modules.Identidad.Domain;

public class RefreshToken
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }

    /// <summary>Hash SHA-256 del token; el valor real solo se le entrega una vez al cliente, nunca se persiste en claro.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiraEn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Revocado { get; set; }
    public DateTime? RevocadoEn { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
