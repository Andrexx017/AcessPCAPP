using AppParque.Services;

namespace AppParque.Features.EvaluacionAccesibilidad;

public enum TipoRespuestaZona
{
    Opciones,
    Numero,
}

/// <param name="Valor">El nombre exacto del miembro del enum que espera el backend, o "Sin"
/// en la prótesis (que allá es null, no un enum). Lo traduce <see cref="PerfilBuilder"/>.</param>
public record OpcionZona(string Texto, string Valor);

/// <param name="Eje">0 = ninguno · 1 estado del cuerpo · 2 dispositivo · 3 capacidad exigida.
/// Es la regla que ordena el vocabulario; se muestra en la pregunta para que el enfermero sepa
/// si está preguntando por el cuerpo, por el aparato o por lo que pide el mecanismo.</param>
/// <param name="DependeDe">Clave de otra pregunta de la misma zona. Si está presente, esta
/// pregunta solo se hace cuando esa otra tomó uno de los <paramref name="ActivaCuando"/>.</param>
public record PreguntaZona(
    string Clave,
    string Titulo,
    string? Ayuda = null,
    int Eje = 0,
    TipoRespuestaZona Tipo = TipoRespuestaZona.Opciones,
    string? Unidad = null,
    IReadOnlyList<OpcionZona>? Opciones = null,
    string? DependeDe = null,
    IReadOnlyList<string>? ActivaCuando = null)
{
    public IReadOnlyList<OpcionZona> OpcionesSeguras => Opciones ?? [];
}

/// <param name="PermiteSinNovedad">false en Medidas: no existe una estatura "normal" por omisión,
/// así que marcarla sin novedad fabricaría un dato. Hay que llenarla.</param>
public record ZonaCorporal(
    string Id,
    string Nombre,
    string Corto,
    string Nota,
    string ResumenSinNovedad,
    bool EnCuerpo,
    IReadOnlyList<PreguntaZona> Preguntas,
    IReadOnlyDictionary<string, string> ValoresSinNovedad,
    bool PermiteSinNovedad = true);

/// <summary>
/// Las zonas en que se reparte el vocabulario de capacidades, con las preguntas de cada una.
///
/// Vive en Modules/CatalogoZonas del lado del servidor (administrable desde Admin → Catálogo del
/// test) y se trae una sola vez por sesión de la app con <see cref="CargarAsync"/> — <see cref="Todas"/>
/// expone esa caché. Las 7 zonas "en el cuerpo" (cabeza, tronco, brazos, piernas, marcha) tienen
/// geometría de píxel fija en MapaCapacidadesViewModel.Geometria y no se pueden agregar/quitar desde
/// el admin, solo editar/reordenar; las 3 "aparte" (medidas, generales, contexto) sí.
/// </summary>
public static class CatalogoZonas
{
    private static IReadOnlyList<ZonaCorporal>? _cache;
    private static Task<IReadOnlyList<ZonaCorporal>>? _cargaEnCurso;

    /// <summary>Las zonas ya cargadas. Llamar a <see cref="CargarAsync"/> antes de leer esto —
    /// lanza si todavía no se cargó ninguna vez en esta sesión de la app.</summary>
    public static IReadOnlyList<ZonaCorporal> Todas => _cache
        ?? throw new InvalidOperationException("CatalogoZonas.CargarAsync() todavía no se ha llamado en esta sesión.");

    /// <summary>
    /// Trae las zonas de <c>GET /api/zonas-capacidades</c> la primera vez que se llama en la sesión
    /// de la app; las siguientes llamadas devuelven la misma caché sin pegarle de nuevo al servidor
    /// (si el admin edita el catálogo, el cambio se ve la próxima vez que se abra la app). Varias
    /// pantallas pueden llamar esto "a la vez" (ej. OnAppearing de MapaCapacidadesView cada vez que
    /// se vuelve de una zona) sin disparar pedidos duplicados.
    /// </summary>
    public static async Task CargarAsync()
    {
        if (_cache is not null)
            return;

        _cargaEnCurso ??= CargarDesdeApiAsync();
        _cache = await _cargaEnCurso;
        _cargaEnCurso = null;
    }

    private static async Task<IReadOnlyList<ZonaCorporal>> CargarDesdeApiAsync()
    {
        var resultado = await ApiClient.GetAsync<List<ZonaCorporal>>("/api/zonas-capacidades");

        if (!resultado.Success)
            throw new ApiException(resultado.ErrorMessage ?? "No se pudo cargar el test de accesibilidad.");

        return resultado.Data;
    }

    public static ZonaCorporal Por(string id) => Todas.First(z => z.Id == id);
}
