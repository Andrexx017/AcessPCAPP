namespace AppParque.Features.RegistroVisitante;

public record VisitanteDto(
    int Id,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    DateTime CreatedAt,
    bool ConsentimientoTratamientoDatos,
    DateTime? FechaConsentimiento);
