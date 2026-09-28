using AppParque.Api.Modules.Visitantes.Domain;

namespace AppParque.Api.Modules.Visitantes.Api;

public record CrearVisitanteRequest(string TipoDocumento, string NumeroDocumento, string Nombre, bool ConsentimientoTratamientoDatos);

public record VisitanteResponse(
    int Id,
    string TipoDocumento,
    string NumeroDocumento,
    string Nombre,
    DateTime CreatedAt,
    bool ConsentimientoTratamientoDatos,
    DateTime? FechaConsentimiento)
{
    public static VisitanteResponse From(Visitante v) => new(
        v.Id, v.TipoDocumento, v.NumeroDocumento, v.Nombre, v.CreatedAt,
        v.ConsentimientoTratamientoDatos, v.FechaConsentimiento);
}
