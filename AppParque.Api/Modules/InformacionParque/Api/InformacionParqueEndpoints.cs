using AppParque.Api.Modules.InformacionParque.Application;

namespace AppParque.Api.Modules.InformacionParque.Api;

public record EstadoParqueResponse(bool Abierto, string Texto, string? HoraApertura, string? HoraCierre)
{
    public static EstadoParqueResponse From(EstadoParque estado) =>
        new(estado.Abierto, estado.Texto, estado.HoraApertura, estado.HoraCierre);
}

public static class InformacionParqueEndpoints
{
    public static void MapInformacionParqueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parque").WithTags("InformacionParque").RequireAuthorization();

        group.MapGet("/horario", async (HorarioParqueService servicio, CancellationToken ct) =>
            Results.Ok(EstadoParqueResponse.From(await servicio.ObtenerEstadoAsync(ct))));
    }
}
