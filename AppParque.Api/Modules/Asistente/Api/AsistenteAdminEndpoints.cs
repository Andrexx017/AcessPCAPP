using AppParque.Api.Data;
using AppParque.Api.Modules.Asistente.Application;
using AppParque.Api.Modules.Asistente.Domain;
using Microsoft.EntityFrameworkCore;

namespace AppParque.Api.Modules.Asistente.Api;

/// <summary>Gestión del bot: proveedores/modelos (con sus keys) y los documentos de contexto.
/// Solo Admin — a diferencia del chat en sí (AsistenteEndpoints), que cualquier usuario usa.</summary>
public static class AsistenteAdminEndpoints
{
    public static void MapAsistenteAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/asistente").WithTags("Admin: Asistente IA").RequireAuthorization("AdminOnly");

        group.MapGet("/proveedores", async (AppDbContext db) =>
            Results.Ok(await db.ProveedoresIA
                .OrderBy(p => p.Prioridad)
                .Select(p => ProveedorIAResponse.From(p))
                .ToListAsync()));

        group.MapPost("/proveedores", async (ProveedorIARequest request, AppDbContext db, AsistenteOrquestador orquestador) =>
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
                return Results.BadRequest(new { error = "La API key es obligatoria para un proveedor nuevo." });

            var proveedor = new ProveedorIA
            {
                Nombre = request.Nombre,
                BaseUrl = request.BaseUrl,
                Modelo = request.Modelo,
                ApiKeyCifrada = orquestador.Cifrar(request.ApiKey),
                Prioridad = request.Prioridad,
                Activo = request.Activo,
                SoportaToolCalling = request.SoportaToolCalling,
                SoportaVision = request.SoportaVision,
            };

            db.ProveedoresIA.Add(proveedor);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/asistente/proveedores/{proveedor.Id}", ProveedorIAResponse.From(proveedor));
        });

        group.MapPut("/proveedores/{id:int}", async (int id, ProveedorIARequest request, AppDbContext db, AsistenteOrquestador orquestador) =>
        {
            var proveedor = await db.ProveedoresIA.FindAsync(id);
            if (proveedor is null)
                return Results.NotFound();

            proveedor.Nombre = request.Nombre;
            proveedor.BaseUrl = request.BaseUrl;
            proveedor.Modelo = request.Modelo;
            proveedor.Prioridad = request.Prioridad;
            proveedor.Activo = request.Activo;
            proveedor.SoportaToolCalling = request.SoportaToolCalling;
            proveedor.SoportaVision = request.SoportaVision;

            if (!string.IsNullOrWhiteSpace(request.ApiKey))
                proveedor.ApiKeyCifrada = orquestador.Cifrar(request.ApiKey);

            await db.SaveChangesAsync();
            return Results.Ok(ProveedorIAResponse.From(proveedor));
        });

        group.MapDelete("/proveedores/{id:int}", async (int id, AppDbContext db) =>
        {
            var proveedor = await db.ProveedoresIA.FindAsync(id);
            if (proveedor is null)
                return Results.NotFound();

            db.ProveedoresIA.Remove(proveedor);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/documentos", async (AppDbContext db) =>
            Results.Ok(await db.DocumentosContexto
                .OrderByDescending(d => d.CargadoEn)
                .Select(d => DocumentoContextoResponse.From(d))
                .ToListAsync()));

        group.MapPost("/documentos", async (DocumentoContextoRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Contenido))
                return Results.BadRequest(new { error = "Nombre y contenido son obligatorios." });

            var documento = new DocumentoContexto
            {
                Nombre = request.Nombre,
                Contenido = request.Contenido,
                Activo = request.Activo,
            };

            db.DocumentosContexto.Add(documento);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/asistente/documentos/{documento.Id}", DocumentoContextoResponse.From(documento));
        });

        group.MapPut("/documentos/{id:int}", async (int id, DocumentoContextoRequest request, AppDbContext db) =>
        {
            var documento = await db.DocumentosContexto.FindAsync(id);
            if (documento is null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.Nombre))
                return Results.BadRequest(new { error = "El nombre es obligatorio." });

            documento.Nombre = request.Nombre;
            // Contenido vacío = no se tocó (el admin solo quería cambiar el nombre o el estado activo).
            if (!string.IsNullOrWhiteSpace(request.Contenido))
                documento.Contenido = request.Contenido;
            documento.Activo = request.Activo;

            await db.SaveChangesAsync();
            return Results.Ok(DocumentoContextoResponse.From(documento));
        });

        group.MapDelete("/documentos/{id:int}", async (int id, AppDbContext db) =>
        {
            var documento = await db.DocumentosContexto.FindAsync(id);
            if (documento is null)
                return Results.NotFound();

            db.DocumentosContexto.Remove(documento);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/consultas-hoy", async (AppDbContext db) =>
        {
            var hoy = DateTime.UtcNow.Date;
            var manana = hoy.AddDays(1);
            var total = await db.MensajesSesion.CountAsync(m => m.Rol == "user" && m.CreadoEn >= hoy && m.CreadoEn < manana);
            return Results.Ok(new ConsultasHoyResponse(total));
        });

        // Los cinco prompts editables (evaluación, consulta de atracciones, horarios, contexto de
        // la app, otros). Son un conjunto fijo de claves (ClavesPrompt), no una lista libre: no
        // hay POST/DELETE, solo lectura (con siembra automática si todavía no existen) y edición
        // del contenido.
        group.MapGet("/prompts", async (AsistenteOrquestador orquestador) =>
            Results.Ok((await orquestador.ObtenerPromptsAsync()).Select(PromptIAResponse.From)));

        group.MapPut("/prompts/{clave}", async (string clave, PromptIARequest request, AppDbContext db, AsistenteOrquestador orquestador) =>
        {
            if (!ClavesPrompt.Todas.Contains(clave))
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.Contenido))
                return Results.BadRequest(new { error = "El contenido no puede quedar vacío." });

            await orquestador.ObtenerPromptsAsync(); // asegura que las 5 filas existan antes de editar

            var prompt = await db.PromptsIA.SingleAsync(p => p.Clave == clave);
            prompt.Contenido = request.Contenido;
            prompt.ActualizadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(PromptIAResponse.From(prompt));
        });
    }
}
