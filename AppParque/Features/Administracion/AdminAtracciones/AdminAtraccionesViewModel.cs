using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminAtracciones;

public class AdminAtraccionesViewModel : BaseViewModel
{
    public ObservableCollection<AtraccionRow> Atracciones { get; } = new();

    // Catálogos usados por el modal de alta/edición (ver AdminAtraccionesView.xaml)
    public ObservableCollection<RestriccionToggleRow> Grupos { get; } = new();
    public ObservableCollection<RestriccionToggleRow> Condiciones { get; } = new();

    public AdminAtraccionesViewModel()
    {
        _ = CargarAsync();
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var resultado = await ApiClient.GetAsync<List<AtraccionAdminDto>>("/api/admin/atracciones");

        Atracciones.Clear();
        if (resultado.Success)
        {
            foreach (var a in resultado.Data)
                Atracciones.Add(new AtraccionRow(a));
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo cargar el catálogo de atracciones.", "OK");
        }

        IsBusy = false;
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

    public string Alturas => Model.AlturaMinima is null && Model.AlturaMaxima is null
        ? "Sin restricción de altura"
        : $"Altura {Model.AlturaMinima?.ToString() ?? "N/A"}–{Model.AlturaMaxima?.ToString() ?? "N/A"} cm";

    public string ResumenBloqueos
    {
        get
        {
            var bloqueados = Model.RestriccionesGrupo.Concat(Model.RestriccionesCondicion).Where(r => !r.Permitido).ToList();
            return bloqueados.Count == 0 ? "Sin bloqueos" : $"{bloqueados.Count} bloqueo(s): {string.Join(", ", bloqueados.Select(b => b.Codigo))}";
        }
    }

    public AtraccionRow(AtraccionAdminDto model)
    {
        Model = model;
    }
}
