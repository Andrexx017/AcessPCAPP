using System.Collections.ObjectModel;
using AppParque.Features.EvaluacionAccesibilidad;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

public class AtraccionDictamenItem
{
    public string Nombre { get; init; } = string.Empty;
    public string Subtitulo { get; init; } = string.Empty;
    public string VeredictoTexto { get; init; } = string.Empty;
    public Color VeredictoFondo { get; init; } = Colors.Transparent;
    public Color VeredictoTexto2 { get; init; } = Colors.Black;

    /// <summary>Los motivos que decidieron el veredicto, ya formateados como viñetas.</summary>
    public string Motivos { get; init; } = string.Empty;
    public bool TieneMotivos { get; init; }

    public string Mitigaciones { get; init; } = string.Empty;
    public bool TieneMitigaciones { get; init; }

    public string Advertencias { get; init; } = string.Empty;
    public bool TieneAdvertencias { get; init; }
}

public class FiltroItem
{
    public string Clave { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
    public bool Activo { get; init; }
    public Color Fondo { get; init; } = Colors.Transparent;
    public Color Trazo { get; init; } = Colors.Gray;
    public Color ColorTexto { get; init; } = Colors.Black;
}

/// <summary>
/// El resultado del motor de capacidades. Vista nueva y no un segundo constructor de
/// ValidacionAtraccionesView porque el resultado cambió de forma: ya no es "preseleccionada sí/no"
/// sino cuatro veredictos con los motivos que los produjeron, y «por verificar» no cabía en un bool.
///
/// También es donde se confirma y persiste el diagnóstico (ver <see cref="ConfirmarAsync"/> y
/// DiagnosticoGuardado en la API) — el ajuste manual del personal (RF-06) por atracción individual
/// sigue sin existir en esta pantalla, eso sí heredado de ValidacionAtraccionesView.
/// </summary>
public class DictamenViewModel : BaseViewModel
{
    private const string TodasClave = "todas";

    private static readonly (string Clave, string Texto)[] Filtros =
    [
        (TodasClave, "Todas"),
        (Veredictos.Permitida, "Permitidas"),
        (Veredictos.ConMitigacion, "Con condición"),
        (Veredictos.PorVerificar, "Por verificar"),
        (Veredictos.Restringida, "No"),
    ];

    private readonly DiagnosticoResponseDto _dictamen;
    private readonly PerfilCapacidadesDto? _perfil;
    private string _filtro = TodasClave;

    public DictamenViewModel(
        DiagnosticoResponseDto dictamen,
        PerfilCapacidadesDto? perfil,
        IReadOnlyList<ResumenZonaItem> diagnosticoSecciones,
        string? condicionAdicional,
        string? respuestaIA = null,
        string? respuestaIAModelo = null,
        Guid? respuestaIASesionId = null)
    {
        _dictamen = dictamen;
        _perfil = perfil;
        DiagnosticoSecciones = diagnosticoSecciones;
        CondicionAdicional = condicionAdicional;
        RespuestaIA = respuestaIA;
        RespuestaIAModelo = respuestaIAModelo;
        RespuestaIASesionId = respuestaIASesionId;
        Refrescar();
    }

    public ObservableCollection<AtraccionDictamenItem> Atracciones { get; } = [];
    public ObservableCollection<FiltroItem> Chips { get; } = [];

    /// <summary>El mismo resumen de respuestas que mostró la pantalla de Resumen — se repite aquí,
    /// condensado, para que el personal vea diagnóstico + preselección + respuesta de la IA
    /// juntos antes de confirmar.</summary>
    public IReadOnlyList<ResumenZonaItem> DiagnosticoSecciones { get; }

    public string? CondicionAdicional { get; }
    public bool TieneCondicionAdicional => !string.IsNullOrWhiteSpace(CondicionAdicional);

    public string? RespuestaIA { get; }
    public string? RespuestaIAModelo { get; }
    public Guid? RespuestaIASesionId { get; }
    public bool TieneRespuestaIA => !string.IsNullOrWhiteSpace(RespuestaIA);

    public string? UltimoErrorGuardado { get; private set; }

    /// <summary>RF-05 persistido (ver DiagnosticoGuardado en la API): guarda perfil, dictamen,
    /// condición adicional y respuesta de la IA en /api/historial. Sin ajuste manual por atracción
    /// todavía — esta pantalla no tiene esa UI (la tenía ValidacionAtraccionesView, del motor viejo).</summary>
    public async Task<bool> ConfirmarAsync()
    {
        if (_perfil is null)
        {
            UltimoErrorGuardado = "No se pudo determinar el perfil evaluado.";
            return false;
        }

        IsBusy = true;

        try
        {
            var resultados = _dictamen.Atracciones
                .Select(a => new ResultadoAtraccionGuardadoDto(a.Codigo, a.Nombre, a.Veredicto, null, null))
                .ToList();

            var request = new CrearDiagnosticoGuardadoRequestDto(
                UsuarioGlobal.VisitanteId,
                _perfil,
                CondicionAdicional,
                resultados,
                _dictamen.Permitidas,
                _dictamen.ConMitigacion,
                _dictamen.PorVerificar,
                _dictamen.Restringidas,
                RespuestaIA,
                RespuestaIAModelo,
                RespuestaIASesionId);

            var resultado = await ApiClient.PostAsync<HistorialDiagnosticoItemDto>("/api/historial", request);

            if (!resultado.Success)
            {
                UltimoErrorGuardado = resultado.ErrorMessage ?? "No se pudo guardar en el historial.";
                return false;
            }

            return true;
        }
        catch (ApiException ex)
        {
            UltimoErrorGuardado = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string NombreVisitante => string.IsNullOrWhiteSpace(UsuarioGlobal.Name)
        ? "Visitante sin nombre"
        : UsuarioGlobal.Name;

    public string Resumen => $"{_dictamen.TotalEvaluadas} atracciones evaluadas";

    public string Permitidas => _dictamen.Permitidas.ToString();
    public string ConMitigacion => _dictamen.ConMitigacion.ToString();
    public string PorVerificar => _dictamen.PorVerificar.ToString();
    public string Restringidas => _dictamen.Restringidas.ToString();

    public bool FaltanDatos => _dictamen.DimensionesSinDato.Count > 0;

    public string TituloFaltantes => _dictamen.DimensionesSinDato.Count == 1
        ? "Falta 1 dato"
        : $"Faltan {_dictamen.DimensionesSinDato.Count} datos";

    /// <summary>
    /// Las dimensiones que quedaron sin responder y que alguna atracción necesitaba. Es la lista de
    /// preguntas que falta hacerle al visitante, y la razón de cada «por verificar».
    /// </summary>
    public string DetalleFaltantes => string.Join("\n", _dictamen.DimensionesSinDato.Select(d => $"·  {d}"));

    public void Filtrar(string clave)
    {
        _filtro = clave;
        Refrescar();
    }

    /// <summary>La atracción, tal como quedó en el dictamen, para armar la preselección antes de
    /// generar el PDF (ver PreseleccionPdfView).</summary>
    public DiagnosticoResponseDto Dictamen => _dictamen;

    private void Refrescar()
    {
        Chips.Clear();
        foreach (var (clave, texto) in Filtros)
        {
            var activo = clave == _filtro;
            Chips.Add(new FiltroItem
            {
                Clave = clave,
                Texto = texto,
                Activo = activo,
                Fondo = activo ? Color.FromArgb("#15191E") : Colors.Transparent,
                Trazo = activo ? Color.FromArgb("#15191E") : Color.FromArgb("#D5DCE4"),
                ColorTexto = activo ? Colors.White : Color.FromArgb("#5A6472"),
            });
        }

        Atracciones.Clear();

        var visibles = _filtro == TodasClave
            ? _dictamen.Atracciones
            : _dictamen.Atracciones.Where(a => a.Veredicto == _filtro).ToList();

        // Lo bloqueante primero: es lo que el enfermero tiene que explicarle al visitante.
        var orden = new Dictionary<string, int>
        {
            [Veredictos.Restringida] = 0,
            [Veredictos.PorVerificar] = 1,
            [Veredictos.ConMitigacion] = 2,
            [Veredictos.Permitida] = 3,
        };

        foreach (var atraccion in visibles.OrderBy(a => orden.TryGetValue(a.Veredicto, out var o) ? o : 9).ThenBy(a => a.Nombre))
            Atracciones.Add(Construir(atraccion));

        OnPropertyChanged(nameof(Atracciones));
    }

    private static AtraccionDictamenItem Construir(DictamenAtraccionDto atraccion)
    {
        var (texto, fondo, color) = atraccion.Veredicto switch
        {
            Veredictos.Permitida => ("Permitida", "#E7F1EF", "#0A4D42"),
            Veredictos.ConMitigacion => ("Con condición", "#E3EBF5", "#163E6E"),
            Veredictos.PorVerificar => ("Verificar en sitio", "#FDF8EC", "#6B4600"),
            Veredictos.Restringida => ("No permitida", "#FBEDEB", "#8A2317"),
            _ => (atraccion.Veredicto, "#EDF0F4", "#5A6472"),
        };

        var subtitulo = atraccion.Rol is "Conductor" or "Pasajero"
            ? $"Intensidad {atraccion.Intensidad} · como {atraccion.Rol.ToLowerInvariant()}"
            : $"Intensidad {atraccion.Intensidad}";

        var motivos = atraccion.Motivos.Select(m => $"·  {m.Texto}").ToList();
        var mitigaciones = atraccion.MitigacionesRequeridas.Select(DictamenPdfMapper.Etiqueta).ToList();

        return new AtraccionDictamenItem
        {
            Nombre = atraccion.Nombre,
            Subtitulo = subtitulo,
            VeredictoTexto = texto,
            VeredictoFondo = Color.FromArgb(fondo),
            VeredictoTexto2 = Color.FromArgb(color),
            Motivos = string.Join("\n", motivos),
            TieneMotivos = motivos.Count > 0,
            Mitigaciones = string.Join("\n", mitigaciones.Select(m => $"·  {m}")),
            TieneMitigaciones = mitigaciones.Count > 0,
            Advertencias = string.Join("\n", atraccion.Advertencias.Select(a => $"·  {a}")),
            TieneAdvertencias = atraccion.Advertencias.Count > 0,
        };
    }
}
