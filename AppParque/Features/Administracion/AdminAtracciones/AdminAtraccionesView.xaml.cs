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

    private void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.FiltrarPorTexto(e.NewTextValue);
    }

    private readonly Dictionary<string, Border> _chipsPorClave = new();

    private void OnFiltroIntensidadTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string clave || sender is not Border chip)
            return;

        // Memoriza los 6 chips la primera vez para poder recolorear el que estaba activo.
        if (_chipsPorClave.Count == 0)
        {
            _chipsPorClave["todas"] = chipTodas;
            _chipsPorClave["baja"] = chipBaja;
            _chipsPorClave["media"] = chipMedia;
            _chipsPorClave["alta"] = chipAlta;
            _chipsPorClave["extrema"] = chipExtrema;
            _chipsPorClave["cultural"] = chipCultural;
        }

        foreach (var (otraClave, otroChip) in _chipsPorClave)
        {
            var activo = otraClave == clave;
            otroChip.BackgroundColor = activo ? Color.FromArgb("#15191E") : Colors.Transparent;
            otroChip.Stroke = activo ? Color.FromArgb("#15191E") : Color.FromArgb("#e3e7ec");
            if (otroChip.Content is Label label)
                label.TextColor = activo ? Colors.White : Color.FromArgb("#5A6472");
        }

        _viewModel.FiltrarPorIntensidad(clave);
    }

    private void OnAtraccionTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: AtraccionRow row })
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
            pickerIntensidad.SelectedIndex = 1; // Media
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
            pickerIntensidad.SelectedIndex = IndiceDeIntensidad(atraccionExistente.Intensidad);
        }

        MostrarTabDatos();
        modalOverlay.IsVisible = true;
        _ = _viewModel.CargarCatalogosAsync(atraccionExistente);
    }

    private void OnCerrarModalClicked(object sender, EventArgs e)
    {
        modalOverlay.IsVisible = false;
    }

    private void OnTabDatosClicked(object sender, EventArgs e) => MostrarTabDatos();

    private void OnTabRestriccionesClicked(object sender, EventArgs e) => MostrarTabRestricciones();

    private void MostrarTabDatos()
    {
        panelDatos.IsVisible = true;
        panelRestricciones.IsVisible = false;
        btnTabDatos.TextColor = Color.FromArgb("#0F6B5C");
        indicadorDatos.BackgroundColor = Color.FromArgb("#0F6B5C");
        btnTabRestricciones.TextColor = Color.FromArgb("#8b93a1");
        indicadorRestricciones.BackgroundColor = Colors.Transparent;
    }

    private void MostrarTabRestricciones()
    {
        panelDatos.IsVisible = false;
        panelRestricciones.IsVisible = true;
        btnTabRestricciones.TextColor = Color.FromArgb("#0F6B5C");
        indicadorRestricciones.BackgroundColor = Color.FromArgb("#0F6B5C");
        btnTabDatos.TextColor = Color.FromArgb("#8b93a1");
        indicadorDatos.BackgroundColor = Colors.Transparent;
    }

    private void OnImagenUrlTextChanged(object sender, TextChangedEventArgs e)
    {
        var url = e.NewTextValue?.Trim();
        var cargable = Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        previewImagenVacio.IsVisible = !cargable;
        imgPreview.IsVisible = cargable;
        if (cargable)
            imgPreview.Source = url;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var nombre = entryNombre.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarTabDatos();
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
            intensidad = CodigoDeIntensidad(pickerIntensidad.SelectedIndex),
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

    // El picker muestra texto capitalizado para el usuario; el contrato con la API usa el código
    // en minúsculas (mismo vocabulario que RequisitosAtraccion.Intensidad del catálogo nuevo).
    private static readonly string[] CodigosIntensidad = ["baja", "media", "alta", "extrema", "cultural"];

    private static string CodigoDeIntensidad(int indicePicker) =>
        indicePicker >= 0 && indicePicker < CodigosIntensidad.Length ? CodigosIntensidad[indicePicker] : "media";

    private static int IndiceDeIntensidad(string? codigo)
    {
        var indice = Array.IndexOf(CodigosIntensidad, codigo?.ToLowerInvariant());
        return indice >= 0 ? indice : 1; // Media si no coincide
    }
}
