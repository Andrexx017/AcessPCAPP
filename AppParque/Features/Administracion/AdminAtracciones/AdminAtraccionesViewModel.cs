using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminAtracciones;

public class AdminAtraccionesViewModel : BaseViewModel
{
    public const string FiltroTodas = "todas";

    private List<AtraccionRow> _todas = [];
    private string _filtroTexto = string.Empty;
    private string _filtroIntensidad = FiltroTodas;

    public ObservableCollection<AtraccionRow> Atracciones { get; } = new();

    // Catálogos usados por el modal de alta/edición (ver AdminAtraccionesView.xaml)
    public ObservableCollection<RestriccionToggleRow> Grupos { get; } = new();
    public ObservableCollection<RestriccionToggleRow> Condiciones { get; } = new();

    public string Subtitulo
    {
        get
        {
            var inactivas = _todas.Count(a => !a.Model.Activa);
            var inactivasTexto = inactivas == 0 ? "" : $" · {inactivas} inactiva{(inactivas == 1 ? "" : "s")}";
            return $"{_todas.Count} atraccion{(_todas.Count == 1 ? "" : "es")}{inactivasTexto}";
        }
    }

    public AdminAtraccionesViewModel()
    {
        _ = CargarAsync();
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var resultado = await ApiClient.GetAsync<List<AtraccionAdminDto>>("/api/admin/atracciones");

        if (resultado.Success)
        {
            _todas = resultado.Data.Select(a => new AtraccionRow(a)).ToList();
            AplicarFiltro();
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo cargar el catálogo de atracciones.", "OK");
        }

        IsBusy = false;
    }

    public void FiltrarPorTexto(string texto)
    {
        _filtroTexto = texto?.Trim() ?? string.Empty;
        AplicarFiltro();
    }

    public void FiltrarPorIntensidad(string clave)
    {
        _filtroIntensidad = clave;
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var visibles = _todas.AsEnumerable();

        if (_filtroIntensidad != FiltroTodas)
            visibles = visibles.Where(a => a.Model.Intensidad == _filtroIntensidad);

        if (_filtroTexto.Length > 0)
            visibles = visibles.Where(a => a.Nombre.Contains(_filtroTexto, StringComparison.OrdinalIgnoreCase));

        Atracciones.Clear();
        foreach (var a in visibles.OrderBy(a => a.Nombre))
            Atracciones.Add(a);

        OnPropertyChanged(nameof(Subtitulo));
    }

    public async Task CargarCatalogosAsync(AtraccionAdminDto? atraccionExistente)
    {
        var gruposResultado = await ApiClient.GetAsync<List<GrupoCatalogoDto>>("/api/admin/grupos");
        var condicionesResultado = await ApiClient.GetAsync<List<CondicionCatalogoDto>>("/api/admin/condiciones");

        if (!gruposResultado.Success || !condicionesResultado.Success)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "No se pudo cargar el catálogo de grupos/condiciones.", "OK");
            return;
        }

        // Sin restricción registrada todavía = permitido por defecto.
        var permitidoGrupo = atraccionExistente?.RestriccionesGrupo.ToDictionary(r => r.Codigo, r => r.Permitido) ?? new();
        var permitidoCondicion = atraccionExistente?.RestriccionesCondicion.ToDictionary(r => r.Codigo, r => r.Permitido) ?? new();

        Grupos.Clear();
        foreach (var g in gruposResultado.Data)
            Grupos.Add(new RestriccionToggleRow
            {
                Codigo = g.Codigo,
                Nombre = g.Nombre,
                Permitido = !permitidoGrupo.TryGetValue(g.Codigo, out var permitido) || permitido,
            });

        Condiciones.Clear();
        foreach (var c in condicionesResultado.Data)
            Condiciones.Add(new RestriccionToggleRow
            {
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Permitido = !permitidoCondicion.TryGetValue(c.Codigo, out var permitido) || permitido,
            });
    }
}

public class RestriccionToggleRow
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public bool Permitido { get; set; }
}

public class AtraccionRow
{
    public AtraccionAdminDto Model { get; }

    public string Nombre => Model.Nombre;

    public string IntensidadTexto => Model.Intensidad switch
    {
        "baja" => "Baja",
        "media" => "Media",
        "alta" => "Alta",
        "extrema" => "Extrema",
        "cultural" => "Cultural",
        _ => Model.Intensidad,
    };

    public Color IntensidadFondo => Model.Intensidad switch
    {
        "baja" => Color.FromArgb("#E7F1EF"),
        "alta" => Color.FromArgb("#FBEDEB"),
        "extrema" => Color.FromArgb("#FBEDEB"),
        "cultural" => Color.FromArgb("#F1EBFD"),
        _ => Color.FromArgb("#FDF8EC"), // media
    };

    public Color IntensidadColorTexto => Model.Intensidad switch
    {
        "baja" => Color.FromArgb("#0A4D42"),
        "alta" => Color.FromArgb("#8A2317"),
        "extrema" => Color.FromArgb("#7A1F12"),
        "cultural" => Color.FromArgb("#4C1D95"),
        _ => Color.FromArgb("#6B4600"), // media
    };

    /// <summary>Color sólido para la franja superior de la tarjeta (más saturado que el badge).</summary>
    public Color IntensidadBanda => Model.Intensidad switch
    {
        "baja" => Color.FromArgb("#0F6B5C"),
        "alta" => Color.FromArgb("#8A2317"),
        "extrema" => Color.FromArgb("#7A1F12"),
        "cultural" => Color.FromArgb("#8b5cf6"),
        _ => Color.FromArgb("#D9A400"), // media
    };

    public string Alturas => Model.AlturaMinima is null && Model.AlturaMaxima is null
        ? "Sin restricción de altura"
        : $"Altura {Model.AlturaMinima?.ToString() ?? "N/A"}–{Model.AlturaMaxima?.ToString() ?? "N/A"} cm";

    public string EstadoTexto => Model.Activa ? "Activa" : "Inactiva";

    public Color EstadoColor => Model.Activa ? Color.FromArgb("#0F6B5C") : Color.FromArgb("#C6CED7");

    public double Opacidad => Model.Activa ? 1.0 : 0.55;

    private List<RestriccionAdminDto> Bloqueos => Model.RestriccionesGrupo.Concat(Model.RestriccionesCondicion).Where(r => !r.Permitido).ToList();

    public string BloqueosTexto => Bloqueos.Count.ToString();

    public Color BloqueosFondo => Bloqueos.Count == 0 ? Color.FromArgb("#E7F1EF") : Color.FromArgb("#FBEDEB");

    public Color BloqueosColorTexto => Bloqueos.Count == 0 ? Color.FromArgb("#0A4D42") : Color.FromArgb("#8A2317");

    public string ResumenBloqueos
    {
        get
        {
            var bloqueados = Bloqueos;
            return bloqueados.Count == 0 ? "Sin bloqueos" : $"{bloqueados.Count} bloqueo(s): {string.Join(", ", bloqueados.Select(b => b.Codigo))}";
        }
    }

    public AtraccionRow(AtraccionAdminDto model)
    {
        Model = model;
    }
}
