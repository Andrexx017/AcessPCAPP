using AppParque.Api.Modules.CatalogoZonas.Domain;

namespace AppParque.Api.Modules.CatalogoZonas.Api;

// ---- Público (GET /api/zonas-capacidades) --------------------------------------------------
// Forma exactamente igual a AppParque/Features/EvaluacionAccesibilidad/CatalogoZonas.cs del
// cliente (ZonaCorporal/PreguntaZona/OpcionZona), para que CatalogoZonas.CargarAsync() deserialice
// esto directo sin una capa de mapeo aparte. Solo viajan zonas/preguntas activas.

public record OpcionZonaPublicResponse(string Texto, string Valor);

public record PreguntaZonaPublicResponse(
    string Clave,
    string Titulo,
    string? Ayuda,
    int Eje,
    string Tipo,
    string? Unidad,
    List<OpcionZonaPublicResponse>? Opciones,
    string? DependeDe,
    List<string>? ActivaCuando);

public record ZonaCorporalPublicResponse(
    string Id,
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool EnCuerpo,
    List<PreguntaZonaPublicResponse> Preguntas,
    Dictionary<string, string> ValoresSinNovedad,
    bool PermiteSinNovedad)
{
    public static ZonaCorporalPublicResponse From(ZonaCorporal zona)
    {
        var preguntasActivas = zona.Preguntas.Where(p => p.Activa).OrderBy(p => p.Orden).ToList();

        return new ZonaCorporalPublicResponse(
            zona.Codigo,
            zona.Nombre,
            zona.Corto,
            zona.Nota,
            zona.ResumenSinNovedad,
            zona.EnCuerpo,
            preguntasActivas.Select(ADto).ToList(),
            preguntasActivas
                .Where(p => p.ValorSinNovedad is not null)
                .ToDictionary(p => p.Clave, p => p.ValorSinNovedad!),
            zona.PermiteSinNovedad);
    }

    private static PreguntaZonaPublicResponse ADto(PreguntaCapacidad p) => new(
        p.Clave,
        p.Titulo,
        p.Ayuda,
        p.Eje,
        p.Tipo.ToString(),
        p.Unidad,
        p.Tipo == TipoRespuestaCapacidad.Opciones
            ? p.Opciones.OrderBy(o => o.Orden).Select(o => new OpcionZonaPublicResponse(o.Texto, o.Valor)).ToList()
            : null,
        p.DependeDeClave,
        string.IsNullOrWhiteSpace(p.ActivaCuandoCsv)
            ? null
            : p.ActivaCuandoCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
}

// ---- Admin (CRUD) ----------------------------------------------------------------------------

public record OpcionCapacidadAdminResponse(int Id, string Texto, string Valor, int Orden)
{
    public static OpcionCapacidadAdminResponse From(OpcionCapacidad o) => new(o.Id, o.Texto, o.Valor, o.Orden);
}

/// <param name="Texto">Único campo editable de una opción ya cableada al motor.</param>
public record OpcionCapacidadAdminRequest(string Texto, string Valor, int Orden);

public record PreguntaCapacidadAdminResponse(
    int Id,
    int ZonaId,
    string Clave,
    string Titulo,
    string? Ayuda,
    int Eje,
    string Tipo,
    string? Unidad,
    int Orden,
    string? DependeDeClave,
    string? ActivaCuandoCsv,
    string? ValorSinNovedad,
    bool Activa,
    bool EsInformativa,
    List<OpcionCapacidadAdminResponse> Opciones)
{
    public static PreguntaCapacidadAdminResponse From(PreguntaCapacidad p, string zonaCodigo) => new(
        p.Id,
        p.ZonaId,
        p.Clave,
        p.Titulo,
        p.Ayuda,
        p.Eje,
        p.Tipo.ToString(),
        p.Unidad,
        p.Orden,
        p.DependeDeClave,
        p.ActivaCuandoCsv,
        p.ValorSinNovedad,
        p.Activa,
        !VocabularioClaves.EstaCableada(zonaCodigo, p.Clave),
        p.Opciones.OrderBy(o => o.Orden).Select(OpcionCapacidadAdminResponse.From).ToList());
}

public record PreguntaCapacidadAdminRequest(
    string Clave,
    string Titulo,
    string? Ayuda,
    int Eje,
    string Tipo,
    string? Unidad,
    int Orden,
    string? DependeDeClave,
    string? ActivaCuandoCsv,
    string? ValorSinNovedad,
    bool Activa);

public record CrearPreguntaCapacidadRequest(
    string Clave,
    string Titulo,
    string? Ayuda,
    int Eje,
    string Tipo,
    string? Unidad,
    int Orden,
    string? DependeDeClave,
    string? ActivaCuandoCsv,
    string? ValorSinNovedad,
    bool Activa,
    List<OpcionCapacidadAdminRequest> Opciones);

public record ZonaCorporalAdminResponse(
    int Id,
    string Codigo,
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool EnCuerpo,
    bool PermiteSinNovedad,
    int Orden,
    bool Activa,
    List<PreguntaCapacidadAdminResponse> Preguntas)
{
    public static ZonaCorporalAdminResponse From(ZonaCorporal z) => new(
        z.Id, z.Codigo, z.Nombre, z.Corto, z.Nota, z.ResumenSinNovedad, z.EnCuerpo, z.PermiteSinNovedad, z.Orden, z.Activa,
        z.Preguntas.OrderBy(p => p.Orden).Select(p => PreguntaCapacidadAdminResponse.From(p, z.Codigo)).ToList());
}

public record ZonaCorporalAdminRequest(
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool PermiteSinNovedad,
    int Orden,
    bool Activa);

/// <summary>Crear una zona solo se permite "aparte" (EnCuerpo = false) — ver VocabularioClaves.ZonasEnCuerpo.</summary>
public record CrearZonaCorporalRequest(
    string Codigo,
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool PermiteSinNovedad,
    int Orden);
