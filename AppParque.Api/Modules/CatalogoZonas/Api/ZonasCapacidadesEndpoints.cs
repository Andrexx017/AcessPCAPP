using AppParque.Api.Data;
using AppParque.Api.Modules.CatalogoZonas.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.CatalogoZonas.Api;

public static class ZonasCapacidadesEndpoints
{
    public static void MapZonasCapacidadesEndpoints(this IEndpointRouteBuilder app)
    {
        // Público: lo consume AppParque/Features/EvaluacionAccesibilidad/CatalogoZonas.cs (cliente)
        // una vez por sesión de la app para armar el test guiado.
        app.MapGet("/api/zonas-capacidades", async (AppDbContext db) =>
        {
            var zonas = await db.ZonasCorporales
                .Include(z => z.Preguntas).ThenInclude(p => p.Opciones)
                .Where(z => z.Activa)
                .OrderBy(z => z.Orden)
                .ToListAsync();

            return Results.Ok(zonas.Select(ZonaCorporalPublicResponse.From).ToList());
        }).WithTags("Zonas de capacidades").RequireAuthorization();
    }

    public static void MapZonasCapacidadesAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/zonas-capacidades").WithTags("Admin: Zonas de capacidades").RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var zonas = await Consulta(db).OrderBy(z => z.Orden).ToListAsync();
            return Results.Ok(zonas.Select(ZonaCorporalAdminResponse.From).ToList());
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var zona = await Consulta(db).SingleOrDefaultAsync(z => z.Id == id);
            return zona is null ? Results.NotFound() : Results.Ok(ZonaCorporalAdminResponse.From(zona));
        });

        group.MapPost("/", async (CrearZonaCorporalRequest request, AppDbContext db) =>
        {
            if (VocabularioClaves.ZonasEnCuerpo.Contains(request.Codigo))
                return Results.BadRequest($"'{request.Codigo}' es el código de una zona en el cuerpo con geometría fija — no se puede crear una zona nueva con ese código.");

            var zona = new ZonaCorporal
            {
                Codigo = request.Codigo,
                Nombre = request.Nombre,
                Corto = request.Corto,
                Nota = request.Nota,
                ResumenSinNovedad = request.ResumenSinNovedad,
                PermiteSinNovedad = request.PermiteSinNovedad,
                Orden = request.Orden,
                EnCuerpo = false, // Solo se crean zonas "aparte" — ver comentario en el contrato.
                Activa = true,
            };

            db.ZonasCorporales.Add(zona);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/zonas-capacidades/{zona.Id}", ZonaCorporalAdminResponse.From(zona)),
                "Ya existe una zona con ese código.");
        });

        group.MapPut("/{id:int}", async (int id, ZonaCorporalAdminRequest request, AppDbContext db) =>
        {
            var zona = await Consulta(db).SingleOrDefaultAsync(z => z.Id == id);
            if (zona is null)
                return Results.NotFound();

            // Codigo y EnCuerpo no se editan: cambiar cualquiera de los dos rompería la geometría
            // del mapa (si es "en el cuerpo") o el vocabulario cableado (VocabularioClaves se indexa
            // por Codigo).
            zona.Nombre = request.Nombre;
            zona.Corto = request.Corto;
            zona.Nota = request.Nota;
            zona.ResumenSinNovedad = request.ResumenSinNovedad;
            zona.PermiteSinNovedad = request.PermiteSinNovedad;
            zona.Orden = request.Orden;
            zona.Activa = request.Activa;

            await db.SaveChangesAsync();
            return Results.Ok(ZonaCorporalAdminResponse.From(zona));
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var zona = await db.ZonasCorporales.FindAsync(id);
            if (zona is null)
                return Results.NotFound();

            if (zona.EnCuerpo)
                return Results.BadRequest("No se puede eliminar una zona \"en el cuerpo\" — tiene geometría fija en el mapa. Desactívala en vez de eliminarla.");

            db.ZonasCorporales.Remove(zona);
            return await DbConflictHelper.TryDeleteAsync(db, "No se puede eliminar: tiene preguntas con respuestas guardadas en el historial.");
        });

        // --- preguntas ---------------------------------------------------------------------------

        group.MapPost("/{zonaId:int}/preguntas", async (int zonaId, CrearPreguntaCapacidadRequest request, AppDbContext db) =>
        {
            var zona = await db.ZonasCorporales.SingleOrDefaultAsync(z => z.Id == zonaId);
            if (zona is null)
                return Results.NotFound();

            if (!Enum.TryParse<TipoRespuestaCapacidad>(request.Tipo, out var tipo))
                return Results.BadRequest($"Tipo inválido: '{request.Tipo}'. Valores válidos: {string.Join(", ", Enum.GetNames<TipoRespuestaCapacidad>())}.");

            var cableada = VocabularioClaves.EstaCableada(zona.Codigo, request.Clave);

            var pregunta = new PreguntaCapacidad
            {
                ZonaId = zonaId,
                Clave = request.Clave,
                Titulo = request.Titulo,
                Ayuda = request.Ayuda,
                Eje = request.Eje,
                Tipo = tipo,
                Unidad = request.Unidad,
                Orden = request.Orden,
                DependeDeClave = request.DependeDeClave,
                ActivaCuandoCsv = request.ActivaCuandoCsv,
                ValorSinNovedad = request.ValorSinNovedad,
                Activa = request.Activa,
            };

            if (cableada && request.Opciones.Count > 0)
                return Results.BadRequest($"'{zona.Codigo}.{request.Clave}' ya está cableada al motor de reglas — no se pueden crear preguntas nuevas con una clave que el motor ya interpreta. Usa otra clave para una pregunta informativa.");

            foreach (var o in request.Opciones)
                pregunta.Opciones.Add(new OpcionCapacidad { Texto = o.Texto, Valor = o.Valor, Orden = o.Orden });

            db.PreguntasCapacidad.Add(pregunta);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/zonas-capacidades/{zonaId}/preguntas/{pregunta.Id}", PreguntaCapacidadAdminResponse.From(pregunta, zona.Codigo)),
                "Ya existe una pregunta con esa clave en esta zona.");
        });

        group.MapPut("/{zonaId:int}/preguntas/{id:int}", async (int zonaId, int id, PreguntaCapacidadAdminRequest request, AppDbContext db) =>
        {
            var pregunta = await db.PreguntasCapacidad.Include(p => p.Zona)
                .SingleOrDefaultAsync(p => p.Id == id && p.ZonaId == zonaId);
            if (pregunta is null)
                return Results.NotFound();

            if (!Enum.TryParse<TipoRespuestaCapacidad>(request.Tipo, out var tipo))
                return Results.BadRequest($"Tipo inválido: '{request.Tipo}'. Valores válidos: {string.Join(", ", Enum.GetNames<TipoRespuestaCapacidad>())}.");

            // La Clave no se edita: es lo que ata (o no) esta pregunta al vocabulario del motor
            // (VocabularioClaves) — cambiarla equivaldría a crear otra pregunta con la identidad de
            // esta, de forma silenciosa.
            pregunta.Titulo = request.Titulo;
            pregunta.Ayuda = request.Ayuda;
            pregunta.Eje = request.Eje;
            pregunta.Tipo = tipo;
            pregunta.Unidad = request.Unidad;
            pregunta.Orden = request.Orden;
            pregunta.DependeDeClave = request.DependeDeClave;
            pregunta.ActivaCuandoCsv = request.ActivaCuandoCsv;
            pregunta.ValorSinNovedad = request.ValorSinNovedad;
            pregunta.Activa = request.Activa;

            await db.SaveChangesAsync();
            return Results.Ok(PreguntaCapacidadAdminResponse.From(pregunta, pregunta.Zona.Codigo));
        });

        group.MapDelete("/{zonaId:int}/preguntas/{id:int}", async (int zonaId, int id, AppDbContext db) =>
        {
            var pregunta = await db.PreguntasCapacidad.SingleOrDefaultAsync(p => p.Id == id && p.ZonaId == zonaId);
            if (pregunta is null)
                return Results.NotFound();

            db.PreguntasCapacidad.Remove(pregunta);
            return await DbConflictHelper.TryDeleteAsync(db, "No se puede eliminar: ya hay respuestas guardadas en el historial para esta pregunta.");
        });

        // --- opciones ----------------------------------------------------------------------------

        group.MapPost("/{zonaId:int}/preguntas/{preguntaId:int}/opciones", async (int zonaId, int preguntaId, OpcionCapacidadAdminRequest request, AppDbContext db) =>
        {
            var pregunta = await db.PreguntasCapacidad.Include(p => p.Zona).Include(p => p.Opciones)
                .SingleOrDefaultAsync(p => p.Id == preguntaId && p.ZonaId == zonaId);
            if (pregunta is null)
                return Results.NotFound();

            if (VocabularioClaves.EstaCableada(pregunta.Zona.Codigo, pregunta.Clave))
                return Results.BadRequest("Esta pregunta está cableada al motor de reglas — no se le pueden agregar opciones nuevas.");

            var opcion = new OpcionCapacidad { Texto = request.Texto, Valor = request.Valor, Orden = request.Orden };
            pregunta.Opciones.Add(opcion);

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Created($"/api/admin/zonas-capacidades/{zonaId}/preguntas/{preguntaId}/opciones/{opcion.Id}", OpcionCapacidadAdminResponse.From(opcion)),
                "Ya existe una opción con ese valor en esta pregunta.");
        });

        group.MapPut("/{zonaId:int}/preguntas/{preguntaId:int}/opciones/{id:int}", async (int zonaId, int preguntaId, int id, OpcionCapacidadAdminRequest request, AppDbContext db) =>
        {
            var opcion = await db.OpcionesCapacidad.Include(o => o.Pregunta).ThenInclude(p => p.Zona)
                .SingleOrDefaultAsync(o => o.Id == id && o.PreguntaId == preguntaId && o.Pregunta.ZonaId == zonaId);
            if (opcion is null)
                return Results.NotFound();

            var cableada = VocabularioClaves.EstaCableada(opcion.Pregunta.Zona.Codigo, opcion.Pregunta.Clave);
            if (cableada && request.Valor != opcion.Valor)
                return Results.BadRequest("Esta opción está cableada al motor de reglas — no se puede cambiar su valor, solo su texto visible.");

            opcion.Texto = request.Texto;
            opcion.Orden = request.Orden;
            if (!cableada)
                opcion.Valor = request.Valor;

            return await DbConflictHelper.TrySaveAsync(
                db,
                () => Results.Ok(OpcionCapacidadAdminResponse.From(opcion)),
                "Ya existe una opción con ese valor en esta pregunta.");
        });

        group.MapDelete("/{zonaId:int}/preguntas/{preguntaId:int}/opciones/{id:int}", async (int zonaId, int preguntaId, int id, AppDbContext db) =>
        {
            var opcion = await db.OpcionesCapacidad.Include(o => o.Pregunta).ThenInclude(p => p.Zona)
                .SingleOrDefaultAsync(o => o.Id == id && o.PreguntaId == preguntaId && o.Pregunta.ZonaId == zonaId);
            if (opcion is null)
                return Results.NotFound();

            if (VocabularioClaves.EstaCableada(opcion.Pregunta.Zona.Codigo, opcion.Pregunta.Clave))
                return Results.BadRequest("Esta opción está cableada al motor de reglas — no se puede eliminar.");

            db.OpcionesCapacidad.Remove(opcion);
            return await DbConflictHelper.TryDeleteAsync(db, "No se puede eliminar: ya está usada en respuestas guardadas en el historial.");
        });
    }

    private static IQueryable<ZonaCorporal> Consulta(AppDbContext db) =>
        db.ZonasCorporales.Include(z => z.Preguntas).ThenInclude(p => p.Opciones);
}
