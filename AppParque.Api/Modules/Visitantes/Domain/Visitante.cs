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

    /// <summary>Acompañante y datos de emergencia: opcionales a nivel de base de datos porque el
    /// registro rápido ("Omitir, pasar al test") no los pide — solo el registro completo los exige,
    /// y esa obligatoriedad se valida en la app, no aquí.</summary>
    public string? NombreAcompanante { get; set; }
    public string? ParentescoAcompanante { get; set; }
    public string? TelefonoAcompanante { get; set; }
    public string? TipoSangre { get; set; }
    public string? Eps { get; set; }
    public string? Alergias { get; set; }

    /// <summary>Aceptación de las políticas de seguridad del parque para el beneficio de acceso
    /// prioritario (VIP) — consentimiento propio del parque, distinto del de tratamiento de datos.</summary>
    public bool AceptaPoliticasParque { get; set; }
    public DateTime? FechaAceptacionPoliticas { get; set; }

    public ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
}
