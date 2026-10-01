using AppParque.Api.Modules.Visitantes.Domain;

namespace AppParque.Api.Modules.Visitantes.Api;

/// <summary>Los campos de acompañante/emergencia y AceptaPoliticasParque son opcionales aquí porque
/// el registro rápido ("Omitir, pasar al test") no los envía — solo el registro completo los llena.</summary>
public record CrearVisitanteRequest(
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    bool ConsentimientoTratamientoDatos,
    string? NombreAcompanante = null,
    string? ParentescoAcompanante = null,
    string? TelefonoAcompanante = null,
    string? TipoSangre = null,
    string? Eps = null,
    string? Alergias = null,
    bool AceptaPoliticasParque = false);

public record VisitanteResponse(
    int Id,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    DateTime CreatedAt,
    bool ConsentimientoTratamientoDatos,
    DateTime? FechaConsentimiento,
    string? NombreAcompanante,
    string? ParentescoAcompanante,
    string? TelefonoAcompanante,
    string? TipoSangre,
    string? Eps,
    string? Alergias,
    bool AceptaPoliticasParque,
    DateTime? FechaAceptacionPoliticas)
{
    public static VisitanteResponse From(Visitante v) => new(
        v.Id, v.TipoDocumento, v.NumeroDocumento, v.Nombre, v.CreatedAt,
        v.ConsentimientoTratamientoDatos, v.FechaConsentimiento,
        v.NombreAcompanante, v.ParentescoAcompanante, v.TelefonoAcompanante,
        v.TipoSangre, v.Eps, v.Alergias,
        v.AceptaPoliticasParque, v.FechaAceptacionPoliticas);
}
