using AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;
using AppParque.Api.Modules.Evaluaciones.Application;

namespace AppParque.Api.Modules.Evaluaciones.Api;

/// <summary>
/// El nuevo RF-05. Convive con <c>/api/evaluaciones</c> (motor de grupos) durante la transición:
/// el historial y el PDF siguen colgando del motor viejo hasta que operaciones valide las 25
/// fichas, y recién entonces se corta. Ver la hoja de ruta del documento de estrategia.
/// </summary>
public static class DiagnosticoEndpoints
{
    public static void MapDiagnosticoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diagnostico").WithTags("Diagnóstico").RequireAuthorization();

        group.MapPost("/", Evaluar);
        group.MapGet("/requisitos", ListarRequisitos);
        group.MapGet("/requisitos/{codigo}", ObtenerRequisitos);
    }

    private static IResult Evaluar(DiagnosticoRequest request, CatalogoRequisitos catalogo)
    {
        var seleccionadas = request.Atracciones is { Count: > 0 }
            ? request.Atracciones.Select(catalogo.Buscar).OfType<RequisitosAtraccion>().ToList()
            : catalogo.Todas.ToList();

        if (seleccionadas.Count == 0)
            return Results.BadRequest(new { error = "Ninguno de los códigos de atracción existe en el catálogo." });

        var dictamen = MotorCapacidades.Evaluar(request.Perfil, seleccionadas);

        return Results.Ok(DiagnosticoMapper.ADto(dictamen, catalogo));
    }

    private static IResult ListarRequisitos(CatalogoRequisitos catalogo) =>
        Results.Ok(catalogo.Todas
            .OrderBy(r => r.Nombre, StringComparer.CurrentCulture)
            .Select(DiagnosticoMapper.ADto)
            .ToList());

    /// <summary>Devuelve la ficha completa, no el resumen: es lo que necesita el módulo de
    /// administración para editarla y lo que usa operaciones para validarla contra el original.</summary>
    private static IResult ObtenerRequisitos(string codigo, CatalogoRequisitos catalogo)
    {
        var requisitos = catalogo.Buscar(codigo);

        return requisitos is null ? Results.NotFound() : Results.Ok(requisitos);
    }
}
