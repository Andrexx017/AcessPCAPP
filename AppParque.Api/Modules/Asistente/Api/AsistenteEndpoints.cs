using System.Security.Claims;
using AppParque.Api.Modules.Asistente.Application;

namespace AppParque.Api.Modules.Asistente.Api;

/// <summary>El chat en sí: cualquier usuario logueado (Admin o Enfermero) lo puede usar — a
/// diferencia de la gestión del bot (AsistenteAdminEndpoints), que es solo de administración.</summary>
public static class AsistenteEndpoints
{
    public static void MapAsistenteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/asistente").WithTags("Asistente").RequireAuthorization();

        group.MapPost("/chat", async (ChatRequest request, ClaimsPrincipal caller, AsistenteOrquestador orquestador, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Mensaje))
                return Results.BadRequest(new { error = "Falta el mensaje." });

            var idTexto = caller.FindFirstValue(ClaimTypes.NameIdentifier) ?? caller.FindFirstValue("sub");
            if (!int.TryParse(idTexto, out var usuarioId))
                return Results.Unauthorized();

            try
            {
                var resultado = await orquestador.ResponderAsync(usuarioId, request.SesionId, request.Mensaje, request.ImagenBase64, request.Modo, ct);
                return Results.Ok(new ChatResponse(resultado.Respuesta, resultado.ModeloUsado, resultado.SesionId));
            }
            catch (InvalidOperationException ex)
            {
                // Ningún proveedor de la cadena pudo responder, o no hay ninguno configurado: no es
                // un 500 de la API, es un estado esperable que el admin tiene que resolver. Se
                // incluye el motivo del último intento (InnerException) porque "ningún proveedor
                // respondió" solo no alcanza para diagnosticar nada.
                var detalle = ex.InnerException is null ? ex.Message : $"{ex.Message} Último motivo: {ex.InnerException.Message}";
                return Results.Problem(detail: detalle, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });
    }
}
