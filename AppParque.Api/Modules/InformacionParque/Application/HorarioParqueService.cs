using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace AppParque.Api.Modules.InformacionParque.Application;

public record EstadoParque(bool Abierto, string Texto, string? HoraApertura, string? HoraCierre);

/// <summary>Un ítem crudo de horarios_A/B.json ("Teleférico Plaza", "telefericos", "9:00AM - 6:30PM").</summary>
public record ItemHorario(
    [property: JsonPropertyName("atraccion")] string Atraccion,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("horario")] string Horario);

/// <summary>
/// Consulta los horarios publicados en informacion.parquedelcafe.co (el sitio oficial de
/// información al visitante): el horario de cada servicio/atracción tal cual, y también un estado
/// general ("¿está el parque abierto ahora?") calculado con el horario de los teleféricos como
/// proxy del horario de puerta a puerta — abren temprano y cierran tarde, a diferencia de
/// taquillas/atracciones puntuales que cierran antes.
///
/// El sitio no tiene una API formal: su propio JS (js/horarios_pnc892.js) elige entre
/// horarios_A.json (entre semana), horarios_B.json (fin de semana) y horarios_C.json (fechas
/// especiales: festivos, temporada alta) según una lista corta de excepciones "dd/mm" → letra que
/// viene hardcodeada en ESE archivo (no en un JSON aparte) y se actualiza cada tanto. Para no
/// quedar desfasados justo esos días (que suelen tener horario extendido), se lee ese mismo
/// archivo en caliente y se le extrae la lista con una regex, en vez de asumir siempre
/// entre-semana/fin-de-semana por DayOfWeek.
/// </summary>
public class HorarioParqueService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<HorarioParqueService> logger)
{
    private const string BaseUrl = "https://informacion.parquedelcafe.co/js/";
    private const string UrlExcepcionesCalendario = BaseUrl + "horarios_pnc892.js";

    private static readonly TimeZoneInfo ZonaColombia = ObtenerZonaColombia();

    private static readonly Regex PatronHorario = new(
        @"(?<horaIni>\d{1,2}:\d{2})\s*(?<ampmIni>AM|PM)\s*-\s*(?<horaFin>\d{1,2}:\d{2})\s*(?<ampmFin>AM|PM)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Extrae {date:"7/10", calendar:"B"} (con o sin espacio tras la llave) de la lista `calendar`
    // del JS del sitio: ver comentario de la clase.
    private static readonly Regex PatronExcepcionCalendario = new(
        @"date:\s*""(?<dia>\d{1,2})/(?<mes>\d{1,2})""\s*,\s*calendar:\s*""(?<letra>[ABC])""",
        RegexOptions.Compiled);

    /// <summary>Todos los servicios/atracciones con su horario de hoy tal cual lo publica el
    /// parque — para la herramienta del Asistente IA, que necesita poder responder sobre
    /// cualquiera de ellos, no solo el estado general.</summary>
    public async Task<IReadOnlyList<ItemHorario>> ObtenerTodosAsync(CancellationToken ct) =>
        await ObtenerHorariosCrudosAsync(await ObtenerLetraCalendarioAsync(ct), ct) ?? [];

    /// <summary>Estado general del parque (abierto/cerrado y a qué hora abre o cierra hoy),
    /// calculado a partir del horario de los teleféricos — para la tarjeta del home.</summary>
    public async Task<EstadoParque> ObtenerEstadoAsync(CancellationToken ct)
    {
        var horarios = await ObtenerHorariosCrudosAsync(await ObtenerLetraCalendarioAsync(ct), ct);
        var horarioCrudo = horarios?.FirstOrDefault(h => h.Tipo == "telefericos")?.Horario;
        if (horarioCrudo is null)
            return new EstadoParque(false, "Horario no disponible en este momento.", null, null);

        var match = PatronHorario.Match(horarioCrudo);
        if (!match.Success)
            return new EstadoParque(false, "Horario no disponible en este momento.", null, null);

        var apertura = ConvertirA24h(match.Groups["horaIni"].Value, match.Groups["ampmIni"].Value);
        var cierre = ConvertirA24h(match.Groups["horaFin"].Value, match.Groups["ampmFin"].Value);
        var horaActual = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaColombia).TimeOfDay;

        var abierto = horaActual >= apertura && horaActual < cierre;
        var texto = abierto
            ? $"Cierra hoy a las {FormatearHora(cierre)}"
            : horaActual < apertura
                ? $"Abre hoy a las {FormatearHora(apertura)}"
                : "Cerrado por hoy";

        return new EstadoParque(abierto, texto, FormatearHora(apertura), FormatearHora(cierre));
    }

    private async Task<string> ObtenerLetraCalendarioAsync(CancellationToken ct)
    {
        var hoy = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ZonaColombia).Date;

        var excepciones = await ObtenerExcepcionesCalendarioAsync(ct);
        var excepcionHoy = excepciones.GetValueOrDefault((hoy.Day, hoy.Month));
        if (excepcionHoy is not null)
            return excepcionHoy;

        return hoy.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "B" : "A";
    }

    /// <summary>La lista de excepciones "(día, mes) → letra" del JS del sitio — vacía si no se
    /// pudo consultar o si el sitio cambió de formato (en ese caso se cae al criterio genérico de
    /// entre-semana/fin-de-semana, igual que antes).</summary>
    private async Task<IReadOnlyDictionary<(int Dia, int Mes), string>> ObtenerExcepcionesCalendarioAsync(CancellationToken ct)
    {
        const string claveCache = "horarios-parque-excepciones-calendario";

        if (cache.TryGetValue<IReadOnlyDictionary<(int, int), string>>(claveCache, out var enCache) && enCache is not null)
            return enCache;

        IReadOnlyDictionary<(int, int), string> resultado = new Dictionary<(int, int), string>();
        try
        {
            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(8);

            var js = await http.GetStringAsync(UrlExcepcionesCalendario, ct);
            resultado = PatronExcepcionCalendario.Matches(js)
                .ToDictionary(
                    m => (int.Parse(m.Groups["dia"].Value), int.Parse(m.Groups["mes"].Value)),
                    m => m.Groups["letra"].Value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo consultar las excepciones de calendario del parque en informacion.parquedelcafe.co.");
        }

        cache.Set(claveCache, resultado, TimeSpan.FromHours(1));
        return resultado;
    }

    /// <summary>La lista completa de horarios_A/B.json de hoy — null si el sitio externo no
    /// respondió o cambió de formato.</summary>
    private async Task<IReadOnlyList<ItemHorario>?> ObtenerHorariosCrudosAsync(string letraCalendario, CancellationToken ct)
    {
        var claveCache = $"horarios-parque-{letraCalendario}";

        if (cache.TryGetValue<IReadOnlyList<ItemHorario>?>(claveCache, out var enCache))
            return enCache;

        IReadOnlyList<ItemHorario>? resultado = null;
        try
        {
            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(8);

            var json = await http.GetStringAsync($"{BaseUrl}horarios_{letraCalendario}.json", ct);
            resultado = JsonSerializer.Deserialize<List<ItemHorario>>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo consultar el horario del parque en informacion.parquedelcafe.co.");
        }

        // Se cachea también el null (sitio caído) un rato corto para no reintentar en cada request
        // de cada pantalla/usuario — la letra de calendario ya cambia sola la clave al cruzar de
        // entre semana a fin de semana.
        cache.Set(claveCache, resultado, TimeSpan.FromHours(1));
        return resultado;
    }

    private static TimeSpan ConvertirA24h(string horaMin, string ampm)
    {
        var partes = horaMin.Split(':');
        var hora = int.Parse(partes[0]);
        var minuto = int.Parse(partes[1]);

        if (ampm.Equals("PM", StringComparison.OrdinalIgnoreCase) && hora != 12) hora += 12;
        if (ampm.Equals("AM", StringComparison.OrdinalIgnoreCase) && hora == 12) hora = 0;

        return new TimeSpan(hora, minuto, 0);
    }

    /// <summary>"6:00 p.m." — a mano en vez de con CultureInfo("es-CO") porque el formato exacto de
    /// AM/PM localizado (espacios, puntos) varía según la versión de datos de cultura del sistema
    /// donde corra el contenedor, y acá conviene que sea siempre igual.</summary>
    private static string FormatearHora(TimeSpan hora)
    {
        var hora12 = hora.Hours % 12;
        if (hora12 == 0) hora12 = 12;
        var sufijo = hora.Hours < 12 ? "a.m." : "p.m.";
        return $"{hora12}:{hora.Minutes:D2} {sufijo}";
    }

    private static TimeZoneInfo ObtenerZonaColombia()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }
        catch (TimeZoneNotFoundException)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); }
            catch (TimeZoneNotFoundException)
            {
                // Colombia no tiene horario de verano: UTC-5 fijo sirve como último recurso.
                return TimeZoneInfo.CreateCustomTimeZone("Colombia-Fijo", TimeSpan.FromHours(-5), "Colombia (fijo)", "Colombia (fijo)");
            }
        }
    }
}
