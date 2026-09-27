using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.Visitantes.Domain;

public class Visitante
{
    public int Id { get; set; }
    public string TipoDocumento { get; set; } = null!;
    public string NumeroDocumento { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
}
