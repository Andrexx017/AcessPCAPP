using AppParque.Services;

namespace AppParque.Features.Administracion.AdminAtracciones;

public partial class AdminAtraccionesView : ContentPage
{
    private readonly AdminAtraccionesViewModel _viewModel = new();
    private AtraccionAdminDto? _atraccionExistente;

    public AdminAtraccionesView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CargarAsync();
    }

    private void OnAtraccionSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        collectionAtracciones.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not AtraccionRow row)
            return;

        AbrirModal(row.Model);
    }

    private void OnNuevaAtraccionClicked(object sender, EventArgs e)
    {
        AbrirModal(atraccionExistente: null);
    }

    private void AbrirModal(AtraccionAdminDto? atraccionExistente)
    {
        _atraccionExistente = atraccionExistente;

        if (atraccionExistente is null)
        {
            lblModalTitulo.Text = "Nueva atracción";
            entryNombre.Text = string.Empty;
            editorDescripcion.Text = string.Empty;
            entryImagenUrl.Text = string.Empty;
            entryAlturaMinima.Text = string.Empty;
            entryAlturaMaxima.Text = string.Empty;
            switchActiva.IsToggled = true;
        }
        else
        {
            lblModalTitulo.Text = "Editar atracción";
            entryNombre.Text = atraccionExistente.Nombre;
            editorDescripcion.Text = atraccionExistente.Descripcion;
            entryImagenUrl.Text = atraccionExistente.ImagenUrl;
            entryAlturaMinima.Text = atraccionExistente.AlturaMinima?.ToString();
            entryAlturaMaxima.Text = atraccionExistente.AlturaMaxima?.ToString();
            switchActiva.IsToggled = atraccionExistente.Activa;
        }

        modalOverlay.IsVisible = true;
        _ = _viewModel.CargarCatalogosAsync(atraccionExistente);
    }

    private void OnCerrarModalClicked(object sender, EventArgs e)
    {
        modalOverlay.IsVisible = false;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var nombre = entryNombre.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlert("Error", "Escriba el nombre de la atracción.", "OK");
            return;
        }

        int? alturaMinima = int.TryParse(entryAlturaMinima.Text, out var min) ? min : null;
        int? alturaMaxima = int.TryParse(entryAlturaMaxima.Text, out var max) ? max : null;

        var body = new
        {
            nombre,
            descripcion = editorDescripcion.Text,
            imagenUrl = entryImagenUrl.Text,
            alturaMinima,
            alturaMaxima,
            activa = switchActiva.IsToggled,
        };

        int atraccionId;

        if (_atraccionExistente is null)
        {
            var resultado = await ApiClient.PostAsync<AtraccionAdminDto>("/api/admin/atracciones", body);
            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo crear la atracción.", "OK");
                return;
            }
            atraccionId = resultado.Data.Id;
        }
        else
        {
            var resultado = await ApiClient.PutAsync<AtraccionAdminDto>($"/api/admin/atracciones/{_atraccionExistente.Id}", body);
            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo actualizar la atracción.", "OK");
                return;
            }
            atraccionId = _atraccionExistente.Id;
        }

        var restriccionesBody = new
        {
            grupos = _viewModel.Grupos.ToDictionary(g => g.Codigo, g => g.Permitido),
            condiciones = _viewModel.Condiciones.ToDictionary(c => c.Codigo, c => c.Permitido),
        };

        var restriccionesResultado = await ApiClient.PutAsync<AtraccionAdminDto>($"/api/admin/atracciones/{atraccionId}/restricciones", restriccionesBody);
        if (!restriccionesResultado.Success)
        {
            await DisplayAlert("Aviso", restriccionesResultado.ErrorMessage ?? "La atracción se guardó, pero no se pudieron guardar las restricciones.", "OK");
            return;
        }

        modalOverlay.IsVisible = false;
        await _viewModel.CargarAsync();
    }
}
