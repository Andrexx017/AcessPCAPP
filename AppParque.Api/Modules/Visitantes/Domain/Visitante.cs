using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.Visitantes.Domain;

public class Visitante
{
    public int Id { get; set; }
    public string TipoDocumento { get; set; } = null!;
    public string NumeroDocumento { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Ley 1581 de 2012 (Habeas Data, Colombia): tratamiento de datos de salud requiere
    /// autorización expresa del titular. Se captura una sola vez al registrar el visitante.</summary>
    public bool ConsentimientoTratamientoDatos { get; set; }
    public DateTime? FechaConsentimiento { get; set; }

    public ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
}
