using System.Collections.ObjectModel;
using System.Text.Json;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Historial;

/// <summary>Una atracción del dictamen guardado, ya con el color de su veredicto — mismo criterio
/// visual que AtraccionDictamenItem en DictamenViewModel, pero sin motivos/mitigaciones porque el
/// historial no los persiste (ver DiagnosticoGuardado.ResultadosJson).</summary>
public class AtraccionDetalleItem
{
    public string Nombre { get; init; } = "";
    public string VeredictoTexto { get; init; } = "";
    public Color VeredictoFondo { get; init; } = Colors.Transparent;
    public Color VeredictoTexto2 { get; init; } = Colors.Black;
    public string? Comentario { get; init; }
    public bool TieneComentario => !string.IsNullOrWhiteSpace(Comentario);
}

/// <summary>Un turno de la conversación de IA de este caso (ver MensajeSesion en la API).</summary>
public class MensajeIaItem
{
    public string Autor { get; init; } = "";
    public string Contenido { get; init; } = "";
    public string Hora { get; init; } = "";
    public bool EsAsistente { get; init; }
}

/// <summary>
/// Carga GET /api/historial/{id} y traduce el detalle a lo que muestra HistorialDetalleView: las
/// respuestas del visitante, el veredicto por atracción agrupado, y la conversación de IA completa
/// (no solo el último RespuestaIATexto que guarda DiagnosticoGuardado).
/// </summary>
public class HistorialDetalleViewModel : BaseViewModel
{
    private readonly int _id;

    public HistorialDetalleViewModel(int id, string visitorNameFallback, string visitorIdFallback)
    {
        _id = id;
        _visitorName = visitorNameFallback;
        _visitorId = visitorIdFallback;
        _ = CargarAsync();
    }

    /// <summary>El detalle ya cargado, para que HistorialView lo reutilice al generar el PDF en vez
    /// de pedirlo otra vez.</summary>
    public HistorialDetalleDto? Detalle { get; private set; }

    private string _visitorName;
    public string VisitorName { get => _visitorName; private set => SetProperty(ref _visitorName, value); }

    private string _visitorId;
    public string VisitorId { get => _visitorId; private set => SetProperty(ref _visitorId, value); }

    private string _enfermero = "";
    public string Enfermero { get => _enfermero; private set => SetProperty(ref _enfermero, value); }

    private string _fecha = "";
    public string Fecha { get => _fecha; private set => SetProperty(ref _fecha, value); }

    private string? _condicionAdicional;
    public string? CondicionAdicional { get => _condicionAdicional; private set => SetProperty(ref _condicionAdicional, value); }
    public bool TieneCondicionAdicional => !string.IsNullOrWhiteSpace(CondicionAdicional);

    private string? _errorCarga;
    public string? ErrorCarga { get => _errorCarga; private set => SetProperty(ref _errorCarga, value); }
    public bool TieneError => !string.IsNullOrWhiteSpace(ErrorCarga);

    public ObservableCollection<DetalleSeccionTexto> PerfilSecciones { get; } = new();
    public ObservableCollection<AtraccionDetalleItem> Permitidas { get; } = new();
    public ObservableCollection<AtraccionDetalleItem> ConMitigacion { get; } = new();
    public ObservableCollection<AtraccionDetalleItem> PorVerificar { get; } = new();
    public ObservableCollection<AtraccionDetalleItem> Restringidas { get; } = new();
    public ObservableCollection<MensajeIaItem> ConsultasIA { get; } = new();

    private bool _tienePerfil;
    public bool TienePerfil { get => _tienePerfil; private set => SetProperty(ref _tienePerfil, value); }

    private bool _tieneConsultasIA;
    public bool TieneConsultasIA { get => _tieneConsultasIA; private set => SetProperty(ref _tieneConsultasIA, value); }

    private bool _tienePermitidas;
    public bool TienePermitidas { get => _tienePermitidas; private set => SetProperty(ref _tienePermitidas, value); }

    private bool _tieneConMitigacion;
    public bool TieneConMitigacion { get => _tieneConMitigacion; private set => SetProperty(ref _tieneConMitigacion, value); }

    private bool _tienePorVerificar;
    public bool TienePorVerificar { get => _tienePorVerificar; private set => SetProperty(ref _tienePorVerificar, value); }

    private bool _tieneRestringidas;
    public bool TieneRestringidas { get => _tieneRestringidas; private set => SetProperty(ref _tieneRestringidas, value); }

    public async Task CargarAsync()
    {
        IsBusy = true;
        var resultado = await ApiClient.GetAsync<HistorialDetalleDto>($"/api/historial/{_id}");

        // Mismo motivo que HistorialViewModel.CargarHistorialAsync: el continuation puede caer en
        // un hilo de threadpool y las ObservableCollection solo se pueden tocar desde el hilo de UI.
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!resultado.Success)
            {
                ErrorCarga = resultado.ErrorMessage ?? "No se pudo cargar el detalle.";
                OnPropertyChanged(nameof(TieneError));
                IsBusy = false;
                return;
            }

            Detalle = resultado.Data;
            VisitorName = resultado.Data.VisitanteNombre;
            VisitorId = resultado.Data.VisitanteNumeroDocumento;
            Enfermero = resultado.Data.Enfermero;
            Fecha = resultado.Data.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            CondicionAdicional = resultado.Data.CondicionAdicional;
            OnPropertyChanged(nameof(TieneCondicionAdicional));

            PerfilSecciones.Clear();
            try
            {
                var perfil = JsonSerializer.Deserialize<PerfilCapacidadesDto>(resultado.Data.PerfilJson, ApiClient.JsonOptions);
                if (perfil is not null)
                    foreach (var seccion in PerfilCapacidadesTextoBuilder.Construir(perfil))
                        PerfilSecciones.Add(seccion);
            }
            catch (JsonException)
            {
                // Perfil ilegible (formato viejo o corrupto): se deja la sección vacía en vez de
                // tumbar toda la pantalla de detalle, que sigue siendo útil sin esto.
            }
            TienePerfil = PerfilSecciones.Count > 0;

            Permitidas.Clear();
            ConMitigacion.Clear();
            PorVerificar.Clear();
            Restringidas.Clear();

            foreach (var r in resultado.Data.Resultados)
            {
                var item = Construir(r);
                var destino = r.Veredicto switch
                {
                    Veredictos.Permitida => Permitidas,
                    Veredictos.ConMitigacion => ConMitigacion,
                    Veredictos.PorVerificar => PorVerificar,
                    Veredictos.Restringida => Restringidas,
                    _ => Restringidas,
                };
                destino.Add(item);
            }

            TienePermitidas = Permitidas.Count > 0;
            TieneConMitigacion = ConMitigacion.Count > 0;
            TienePorVerificar = PorVerificar.Count > 0;
            TieneRestringidas = Restringidas.Count > 0;

            ConsultasIA.Clear();
            foreach (var m in resultado.Data.ConsultasIA)
            {
                ConsultasIA.Add(new MensajeIaItem
                {
                    Autor = m.Rol == "assistant" ? "Asistente IA" : "Personal",
                    Contenido = m.Contenido,
                    Hora = m.CreadoEn.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    EsAsistente = m.Rol == "assistant",
                });
            }
            TieneConsultasIA = ConsultasIA.Count > 0;

            IsBusy = false;
        });
    }

    private static AtraccionDetalleItem Construir(ResultadoAtraccionGuardadoDto r)
    {
        var (texto, fondo, color) = r.Veredicto switch
        {
            Veredictos.Permitida => ("Permitida", "#E7F1EF", "#0A4D42"),
            Veredictos.ConMitigacion => ("Con condición", "#E3EBF5", "#163E6E"),
            Veredictos.PorVerificar => ("Verificar en sitio", "#FDF8EC", "#6B4600"),
            Veredictos.Restringida => ("No permitida", "#FBEDEB", "#8A2317"),
            _ => (r.Veredicto, "#EDF0F4", "#5A6472"),
        };

        return new AtraccionDetalleItem
        {
            Nombre = r.NombreAtraccion,
            VeredictoTexto = texto,
            VeredictoFondo = Color.FromArgb(fondo),
            VeredictoTexto2 = Color.FromArgb(color),
            Comentario = r.Comentario,
        };
    }
}
