using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AppParque.Api.Data;
using AppParque.Api.Modules.Evaluaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Evaluaciones.Api;

/// <summary>
/// Persistencia del motor nuevo (ver DiagnosticoGuardado): lo que el personal confirma en la
/// pantalla de preselección/diagnóstico después de contestar el test de zonas. Convive con
/// <c>/api/evaluaciones</c> (el flujo de "visitante recurrente, reutilizar evaluación" sigue
/// escribiendo ahí) hasta que ese camino también se migre.
/// </summary>
public static class HistorialEndpoints
{
    public static void MapHistorialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/historial").WithTags("Historial").RequireAuthorization();

        group.MapGet("/", ListarAsync);
        group.MapGet("/{id:int}", ObtenerDetalleAsync);
        group.MapPost("/", CrearAsync);
    }

    /// <summary>RF-08: Enfermero ve solo lo suyo, Admin ve todo — mismo criterio que
    /// EvaluacionesEndpoints.ListarHistorialAsync.</summary>
    private static async Task<IResult> ListarAsync(AppDbContext db, ClaimsPrincipal user, string? query)
    {
        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var esAdmin = user.IsInRole("Admin");

        var diagnosticos = db.DiagnosticosGuardados
            .Include(d => d.Visitante)
            .Include(d => d.Usuario)
            .AsQueryable();

        if (!esAdmin)
            diagnosticos = diagnosticos.Where(d => d.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(query))
            diagnosticos = diagnosticos.Where(d =>
                d.Visitante.Nombre.Contains(query) || d.Visitante.NumeroDocumento.Contains(query));

        var resultado = await diagnosticos
            .OrderByDescending(d => d.Fecha)
            .Select(d => new HistorialDiagnosticoItemResponse(
                d.Id,
                d.VisitanteId,
                d.Visitante.Nombre,
                d.Visitante.NumeroDocumento,
                d.UsuarioId,
                d.Usuario.NombreCompleto,
                d.Fecha,
                d.Permitidas,
                d.ConMitigacion,
                d.PorVerificar,
                d.Restringidas,
                !string.IsNullOrWhiteSpace(d.CondicionAdicional),
                !string.IsNullOrWhiteSpace(d.RespuestaIATexto)))
            .ToListAsync();

        return Results.Ok(resultado);
    }

    /// <summary>RF-08 también aquí: un enfermero no puede leer el detalle de un diagnóstico que no
    /// es suyo, ni siquiera conociendo el id (se responde 404 y no 403, para no confirmar que el
    /// id existe).</summary>
    private static async Task<IResult> ObtenerDetalleAsync(int id, AppDbContext db, ClaimsPrincipal user)
    {
        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var esAdmin = user.IsInRole("Admin");

        var diagnostico = await db.DiagnosticosGuardados
            .Include(d => d.Visitante)
            .Include(d => d.Usuario)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (diagnostico is null || (!esAdmin && diagnostico.UsuarioId != usuarioId))
            return Results.NotFound();

        var resultados = JsonSerializer.Deserialize<List<ResultadoAtraccionGuardado>>(diagnostico.ResultadosJson) ?? [];

        var mensajes = diagnostico.RespuestaIASesionId is { } sesionId
            ? await db.MensajesSesion
                .Where(m => m.SesionId == sesionId)
                .OrderBy(m => m.CreadoEn)
                .ToListAsync()
            : [];

        return Results.Ok(new HistorialDetalleResponse(
            diagnostico.Id,
            diagnostico.VisitanteId,
            diagnostico.Visitante.Nombre,
            diagnostico.Visitante.NumeroDocumento,
            diagnostico.UsuarioId,
            diagnostico.Usuario.NombreCompleto,
            diagnostico.Fecha,
            diagnostico.Permitidas,
            diagnostico.ConMitigacion,
            diagnostico.PorVerificar,
            diagnostico.Restringidas,
            diagnostico.CondicionAdicional,
            diagnostico.PerfilJson,
            resultados.Select(r => new ResultadoAtraccionGuardadoResponse(r.CodigoAtraccion, r.NombreAtraccion, r.Veredicto, r.AjusteManual, r.Comentario)).ToList(),
            diagnostico.RespuestaIATexto,
            diagnostico.RespuestaIAModelo,
            mensajes.Select(m => new MensajeIaResponse(m.Rol, m.Contenido, m.CreadoEn)).ToList()));
    }

    private static async Task<IResult> CrearAsync(CrearDiagnosticoGuardadoRequest request, AppDbContext db, ClaimsPrincipal user)
    {
        var visitante = await db.Visitantes.FindAsync(request.VisitanteId);
        if (visitante is null)
            return Results.NotFound($"No existe el visitante {request.VisitanteId}.");

        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario is null)
            return Results.Unauthorized();

        var diagnostico = new DiagnosticoGuardado
        {
            VisitanteId = visitante.Id,
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            PerfilJson = request.Perfil.GetRawText(),
            CondicionAdicional = string.IsNullOrWhiteSpace(request.CondicionAdicional) ? null : request.CondicionAdicional,
            ResultadosJson = JsonSerializer.Serialize(request.Resultados.Select(r =>
                new ResultadoAtraccionGuardado(r.CodigoAtraccion, r.NombreAtraccion, r.Veredicto, r.AjusteManual, r.Comentario))),
            Permitidas = request.Permitidas,
            ConMitigacion = request.ConMitigacion,
            PorVerificar = request.PorVerificar,
            Restringidas = request.Restringidas,
            RespuestaIATexto = string.IsNullOrWhiteSpace(request.RespuestaIATexto) ? null : request.RespuestaIATexto,
            RespuestaIAModelo = request.RespuestaIAModelo,
            RespuestaIASesionId = request.RespuestaIASesionId,
        };

        db.DiagnosticosGuardados.Add(diagnostico);
        await db.SaveChangesAsync();

        return Results.Created($"/api/historial/{diagnostico.Id}", new HistorialDiagnosticoItemResponse(
            diagnostico.Id,
            diagnostico.VisitanteId,
            visitante.Nombre,
            visitante.NumeroDocumento,
            diagnostico.UsuarioId,
            usuario.NombreCompleto,
            diagnostico.Fecha,
            diagnostico.Permitidas,
            diagnostico.ConMitigacion,
            diagnostico.PorVerificar,
            diagnostico.Restringidas,
            diagnostico.CondicionAdicional is not null,
            diagnostico.RespuestaIATexto is not null));
    }
}
