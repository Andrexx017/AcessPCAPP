using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AppParque.Api.Data;
using AppParque.Api.Modules.Evaluaciones.Application;
using AppParque.Api.Modules.Evaluaciones.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Evaluaciones.Api;

public static class EvaluacionesEndpoints
{
    public static void MapEvaluacionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluaciones").WithTags("Evaluaciones").RequireAuthorization();

        group.MapGet("/", ListarHistorialAsync);
        group.MapPost("/", CrearAsync);
        group.MapGet("/{id:int}", ObtenerAsync);
        group.MapPut("/{id:int}/atracciones/{atraccionId:int}", AjustarAsync);
        group.MapPost("/{id:int}/reutilizar", ReutilizarAsync);
    }

    /// <summary>RF-08: Enfermero ve solo su propio historial, Admin ve todo. Filtra por nombre o
    /// número de documento del visitante con `query`.</summary>
    private static async Task<IResult> ListarHistorialAsync(AppDbContext db, ClaimsPrincipal user, string? query)
    {
        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var esAdmin = user.IsInRole("Admin");

        var evaluaciones = db.Evaluaciones
            .Include(e => e.Visitante)
            .Include(e => e.Usuario)
            .Include(e => e.Grupos).ThenInclude(g => g.Grupo)
            .Include(e => e.Condiciones).ThenInclude(c => c.Condicion)
            .AsQueryable();

        if (!esAdmin)
            evaluaciones = evaluaciones.Where(e => e.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(query))
            evaluaciones = evaluaciones.Where(e =>
                e.Visitante.Nombre.Contains(query) || e.Visitante.NumeroDocumento.Contains(query));

        var resultado = await evaluaciones
            .OrderByDescending(e => e.Fecha)
            .Select(e => new HistorialItemResponse(
                e.Id,
                e.VisitanteId,
                e.Visitante.Nombre,
                e.Visitante.NumeroDocumento,
                e.UsuarioId,
                e.Usuario.NombreCompleto,
                e.Fecha,
                e.Estatura,
                e.Grupos.Select(g => new RestriccionDto(g.Grupo.Codigo, g.Grupo.Nombre)).ToList(),
                e.Condiciones.Select(c => new RestriccionDto(c.Condicion.Codigo, c.Condicion.Nombre)).ToList()))
            .ToListAsync();

        return Results.Ok(resultado);
    }

    private static async Task<IResult> CrearAsync(CrearEvaluacionRequest request, AppDbContext db, ClaimsPrincipal user)
    {
        var visitante = await db.Visitantes.FindAsync(request.VisitanteId);
        if (visitante is null)
            return Results.NotFound($"No existe el visitante {request.VisitanteId}.");

        var opcionesIds = request.OpcionesRespuestaIds?.Distinct().ToList() ?? [];
        if (opcionesIds.Count == 0)
            return Results.BadRequest("Debe enviar al menos una respuesta del test.");

        var opciones = await db.OpcionesRespuesta
            .Where(o => opcionesIds.Contains(o.Id))
            .ToListAsync();

        if (opciones.Count != opcionesIds.Count)
            return Results.BadRequest("Una o más opciones de respuesta no existen.");

        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var codigosRespuesta = opciones.Select(o => o.Codigo).ToHashSet();
        var (gruposActivados, condicionesActivadas) = MotorReglas.Evaluar(codigosRespuesta);

        var grupos = await db.Grupos.Where(g => gruposActivados.Contains(g.Codigo)).ToListAsync();
        var condiciones = await db.Condiciones.Where(c => condicionesActivadas.Contains(c.Codigo)).ToListAsync();

        var atracciones = await db.Atracciones
            .Where(a => a.Activa)
            .Include(a => a.RestriccionesGrupo).ThenInclude(r => r.Grupo)
            .Include(a => a.RestriccionesCondicion).ThenInclude(r => r.Condicion)
            .ToListAsync();

        var evaluacion = new Evaluacion
        {
            VisitanteId = visitante.Id,
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Edad = request.Edad,
            Estatura = request.Estatura,
        };

        foreach (var opcion in opciones)
            evaluacion.Respuestas.Add(new EvaluacionRespuesta { OpcionRespuestaId = opcion.Id });

        foreach (var grupo in grupos)
            evaluacion.Grupos.Add(new EvaluacionGrupo { GrupoId = grupo.Id });

        foreach (var condicion in condiciones)
            evaluacion.Condiciones.Add(new EvaluacionCondicion { CondicionId = condicion.Id });

        foreach (var atraccion in atracciones)
        {
            var esSegura = PreseleccionService.EsSeguraPara(atraccion, gruposActivados, condicionesActivadas, request.Estatura);
            evaluacion.AtraccionResultados.Add(new EvaluacionAtraccionResultado
            {
                AtraccionId = atraccion.Id,
                PreseleccionadaAutomatica = esSegura,
            });
        }

        db.Evaluaciones.Add(evaluacion);
        await db.SaveChangesAsync();

        var response = await BuildResponseAsync(db, evaluacion.Id);
        return Results.Created($"/api/evaluaciones/{evaluacion.Id}", response);
    }

    /// <summary>RF-14: cuando el visitante confirma que sus condiciones siguen igual que en una
    /// evaluación anterior, no se repite el test — se copian los grupos/condiciones ya activados y
    /// se recalcula la preselección contra el catálogo ACTUAL de atracciones (que pudo cambiar desde
    /// la evaluación original, aunque el visitante no haya cambiado).</summary>
    private static async Task<IResult> ReutilizarAsync(int id, ReutilizarEvaluacionRequest request, AppDbContext db, ClaimsPrincipal user)
    {
        var origen = await db.Evaluaciones
            .Include(e => e.Grupos).ThenInclude(g => g.Grupo)
            .Include(e => e.Condiciones).ThenInclude(c => c.Condicion)
            .SingleOrDefaultAsync(e => e.Id == id);

        if (origen is null)
            return Results.NotFound($"No existe la evaluación {id}.");

        var usuarioIdTexto = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(usuarioIdTexto, out var usuarioId))
            return Results.Unauthorized();

        var gruposActivados = origen.Grupos.Select(g => g.Grupo.Codigo).ToHashSet();
        var condicionesActivadas = origen.Condiciones.Select(c => c.Condicion.Codigo).ToHashSet();
        var estatura = request.Estatura ?? origen.Estatura;

        var atracciones = await db.Atracciones
            .Where(a => a.Activa)
            .Include(a => a.RestriccionesGrupo).ThenInclude(r => r.Grupo)
            .Include(a => a.RestriccionesCondicion).ThenInclude(r => r.Condicion)
            .ToListAsync();

        var evaluacion = new Evaluacion
        {
            VisitanteId = origen.VisitanteId,
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Edad = request.Edad ?? origen.Edad,
            Estatura = estatura,
        };

        foreach (var grupo in origen.Grupos)
            evaluacion.Grupos.Add(new EvaluacionGrupo { GrupoId = grupo.GrupoId });

        foreach (var condicion in origen.Condiciones)
            evaluacion.Condiciones.Add(new EvaluacionCondicion { CondicionId = condicion.CondicionId });

        foreach (var atraccion in atracciones)
        {
            var esSegura = PreseleccionService.EsSeguraPara(atraccion, gruposActivados, condicionesActivadas, estatura);
            evaluacion.AtraccionResultados.Add(new EvaluacionAtraccionResultado
            {
                AtraccionId = atraccion.Id,
                PreseleccionadaAutomatica = esSegura,
            });
        }

        db.Evaluaciones.Add(evaluacion);
        await db.SaveChangesAsync();

        var response = await BuildResponseAsync(db, evaluacion.Id);
        return Results.Created($"/api/evaluaciones/{evaluacion.Id}", response);
    }

    private static async Task<IResult> ObtenerAsync(int id, AppDbContext db)
    {
        var response = await BuildResponseAsync(db, id);
        return response is null ? Results.NotFound() : Results.Ok(response);
    }

    private static async Task<IResult> AjustarAsync(int id, int atraccionId, AjustarPreseleccionRequest request, AppDbContext db)
    {
        var resultado = await db.EvaluacionAtraccionResultados
            .SingleOrDefaultAsync(r => r.EvaluacionId == id && r.AtraccionId == atraccionId);

        if (resultado is null)
            return Results.NotFound();

        resultado.ValidadaPersonal = request.ValidadaPersonal;
        resultado.Comentario = request.Comentario;

        await db.SaveChangesAsync();

        var response = await BuildResponseAsync(db, id);
        return Results.Ok(response);
    }

    private static async Task<EvaluacionResponse?> BuildResponseAsync(AppDbContext db, int evaluacionId)
    {
        var evaluacion = await db.Evaluaciones
            .Include(e => e.Grupos).ThenInclude(g => g.Grupo)
            .Include(e => e.Condiciones).ThenInclude(c => c.Condicion)
            .Include(e => e.AtraccionResultados).ThenInclude(r => r.Atraccion)
            .SingleOrDefaultAsync(e => e.Id == evaluacionId);

        if (evaluacion is null)
            return null;

        return new EvaluacionResponse(
            evaluacion.Id,
            evaluacion.VisitanteId,
            evaluacion.UsuarioId,
            evaluacion.Fecha,
            evaluacion.Edad,
            evaluacion.Estatura,
            evaluacion.Grupos.Select(g => g.Grupo.Codigo).ToList(),
            evaluacion.Condiciones.Select(c => c.Condicion.Codigo).ToList(),
            evaluacion.AtraccionResultados
                .OrderBy(r => r.Atraccion.Nombre)
                .Select(r => new AtraccionResultadoResponse(
                    r.AtraccionId,
                    r.Atraccion.Nombre,
                    r.Atraccion.Descripcion,
                    r.Atraccion.ImagenUrl,
                    r.Atraccion.AlturaMinima,
                    r.Atraccion.AlturaMaxima,
                    r.PreseleccionadaAutomatica,
                    r.ValidadaPersonal,
                    r.Comentario))
                .ToList());
    }
}
