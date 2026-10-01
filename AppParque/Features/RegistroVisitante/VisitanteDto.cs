namespace AppParque.Features.RegistroVisitante;

public record VisitanteDto(
    int Id,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    DateTime CreatedAt,
    bool ConsentimientoTratamientoDatos,
    DateTime? FechaConsentimiento,
    string? NombreAcompanante = null,
    string? ParentescoAcompanante = null,
    string? TelefonoAcompanante = null,
    string? TipoSangre = null,
    string? Eps = null,
    string? Alergias = null,
    bool AceptaPoliticasParque = false,
    DateTime? FechaAceptacionPoliticas = null);
