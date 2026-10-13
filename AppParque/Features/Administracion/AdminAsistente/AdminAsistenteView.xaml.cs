using AppParque.Services;
using AppParque.Shared.Models;

namespace AppParque.Features.Administracion.AdminAsistente;

public partial class AdminAsistenteView : ContentPage
{
    private readonly AdminAsistenteViewModel _viewModel = new();
    private ProveedorIADto? _proveedorExistente;
    private DocumentoContextoDto? _documentoExistente;

    public AdminAsistenteView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.CargarAsync();
    }

    // --- pestañas ---------------------------------------------------------------------------

    private void OnTabProveedoresClicked(object sender, EventArgs e) => MostrarTab(panelProveedores);

    private void OnTabDocumentosClicked(object sender, EventArgs e) => MostrarTab(panelDocumentos);

    private void OnTabPromptsClicked(object sender, EventArgs e) => MostrarTab(panelPrompts);

    private void MostrarTab(View panelActivo)
    {
        var paneles = new[] { panelProveedores, panelDocumentos, panelPrompts };
        var botones = new[] { btnTabProveedores, btnTabDocumentos, btnTabPrompts };
        var indicadores = new[] { indicadorProveedores, indicadorDocumentos, indicadorPrompts };

        for (var i = 0; i < paneles.Length; i++)
        {
            var activo = paneles[i] == panelActivo;
            paneles[i].IsVisible = activo;
            botones[i].TextColor = activo ? Color.FromArgb("#0F6B5C") : Color.FromArgb("#8b93a1");
            indicadores[i].BackgroundColor = activo ? Color.FromArgb("#0F6B5C") : Colors.Transparent;
        }
    }

    // --- proveedores --------------------------------------------------------------------------

    private void OnProveedorSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        collectionProveedores.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not ProveedorRow row)
            return;

        AbrirModalProveedor(row.Model);
    }

    private void OnNuevoProveedorClicked(object sender, EventArgs e) => AbrirModalProveedor(null);

    private void AbrirModalProveedor(ProveedorIADto? existente)
    {
        _proveedorExistente = existente;

        if (existente is null)
        {
            lblModalProveedorTitulo.Text = "Nuevo proveedor";
            entryProvNombre.Text = string.Empty;
            entryProvBaseUrl.Text = "https://api.groq.com/openai/v1";
            entryProvModelo.Text = string.Empty;
            entryProvApiKey.Text = string.Empty;
            entryProvApiKey.Placeholder = "gsk_...";
            lblProvApiKeyNota.IsVisible = false;
            entryProvPrioridad.Text = (_viewModel.Proveedores.Count + 1).ToString();
            switchProvActivo.IsToggled = true;
            switchProvToolCalling.IsToggled = true;
            switchProvVision.IsToggled = false;
            btnEliminarProveedor.IsVisible = false;
        }
        else
        {
            lblModalProveedorTitulo.Text = "Editar proveedor";
            entryProvNombre.Text = existente.Nombre;
            entryProvBaseUrl.Text = existente.BaseUrl;
            entryProvModelo.Text = existente.Modelo;
            entryProvApiKey.Text = string.Empty;
            entryProvApiKey.Placeholder = "•••• configurada";
            lblProvApiKeyNota.IsVisible = true;
            entryProvPrioridad.Text = existente.Prioridad.ToString();
            switchProvActivo.IsToggled = existente.Activo;
            switchProvToolCalling.IsToggled = existente.SoportaToolCalling;
            switchProvVision.IsToggled = existente.SoportaVision;
            btnEliminarProveedor.IsVisible = true;
        }

        modalProveedor.IsVisible = true;
    }

    private void OnCerrarModalProveedorClicked(object sender, EventArgs e) => modalProveedor.IsVisible = false;

    private async void OnGuardarProveedorClicked(object sender, EventArgs e)
    {
        var nombre = entryProvNombre.Text?.Trim();
        var baseUrl = entryProvBaseUrl.Text?.Trim();
        var modelo = entryProvModelo.Text?.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(modelo))
        {
            await DisplayAlert("Error", "Nombre, URL base y modelo son obligatorios.", "OK");
            return;
        }

        if (_proveedorExistente is null && string.IsNullOrWhiteSpace(entryProvApiKey.Text))
        {
            await DisplayAlert("Error", "La API key es obligatoria para un proveedor nuevo.", "OK");
            return;
        }

        if (!int.TryParse(entryProvPrioridad.Text, out var prioridad))
            prioridad = 99;

        var body = new ProveedorIARequestDto(
            nombre, baseUrl, modelo,
            string.IsNullOrWhiteSpace(entryProvApiKey.Text) ? null : entryProvApiKey.Text,
            prioridad, switchProvActivo.IsToggled, switchProvToolCalling.IsToggled, switchProvVision.IsToggled);

        var resultado = _proveedorExistente is null
            ? await ApiClient.PostAsync<ProveedorIADto>("/api/admin/asistente/proveedores", body)
            : await ApiClient.PutAsync<ProveedorIADto>($"/api/admin/asistente/proveedores/{_proveedorExistente.Id}", body);

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo guardar el proveedor.", "OK");
            return;
        }

        modalProveedor.IsVisible = false;
        await _viewModel.CargarAsync();
    }

    private async void OnEliminarProveedorClicked(object sender, EventArgs e)
    {
        if (_proveedorExistente is null)
            return;

        var confirmar = await DisplayAlert("Eliminar proveedor", $"¿Quitar \"{_proveedorExistente.Nombre}\" de la cadena de intentos?", "Sí", "No");
        if (!confirmar)
            return;

        var resultado = await ApiClient.DeleteAsync($"/api/admin/asistente/proveedores/{_proveedorExistente.Id}");
        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar.", "OK");
            return;
        }

        modalProveedor.IsVisible = false;
        await _viewModel.CargarAsync();
    }

    // --- documentos de contexto ----------------------------------------------------------------

    private void OnDocumentoSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        collectionDocumentos.SelectedItem = null;

        if (e.CurrentSelection.FirstOrDefault() is not DocumentoRow row)
            return;

        // El contenido completo no viaja en la lista (solo la longitud), así que para editar hay
        // que pedirlo: se resuelve simplemente borrando y creando de nuevo, más simple para una
        // guía de texto que el admin puede volver a pegar o recargar desde archivo.
        _documentoExistente = row.Model;
        entryDocNombre.Text = row.Model.Nombre;
        editorDocContenido.Text = string.Empty;
        editorDocContenido.Placeholder = "Deja vacío y guarda solo para cambiar el estado activo/inactivo, o pega el contenido nuevo para reemplazarlo.";
        switchDocActivo.IsToggled = row.Model.Activo;
        btnEliminarDocumento.IsVisible = true;
        modalDocumento.IsVisible = true;
    }

    private void OnNuevoDocumentoClicked(object sender, EventArgs e)
    {
        _documentoExistente = null;
        entryDocNombre.Text = string.Empty;
        editorDocContenido.Text = string.Empty;
        editorDocContenido.Placeholder = "Pega el texto de la guía, o cárgalo desde un archivo arriba.";
        switchDocActivo.IsToggled = true;
        btnEliminarDocumento.IsVisible = false;
        modalDocumento.IsVisible = true;
    }

    private void OnCerrarModalDocumentoClicked(object sender, EventArgs e) => modalDocumento.IsVisible = false;

    private async void OnCargarArchivoClicked(object sender, EventArgs e)
    {
        try
        {
            var archivo = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Elige una guía (.txt, .md)" });
            if (archivo is null)
                return;

            using var stream = await archivo.OpenReadAsync();
            using var lector = new StreamReader(stream);
            var texto = await lector.ReadToEndAsync();

            editorDocContenido.Text = texto;
            if (string.IsNullOrWhiteSpace(entryDocNombre.Text))
                entryDocNombre.Text = Path.GetFileNameWithoutExtension(archivo.FileName);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo leer el archivo: {ex.Message}", "OK");
        }
    }

    private async void OnGuardarDocumentoClicked(object sender, EventArgs e)
    {
        var nombre = entryDocNombre.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            await DisplayAlert("Error", "Escribe un nombre para el documento.", "OK");
            return;
        }

        var contenido = editorDocContenido.Text;

        if (_documentoExistente is null && string.IsNullOrWhiteSpace(contenido))
        {
            await DisplayAlert("Error", "El contenido no puede quedar vacío.", "OK");
            return;
        }

        // Al editar, contenido vacío en el Editor = "no lo cambié": el servidor conserva el actual.
        var body = new DocumentoContextoRequestDto(nombre, contenido ?? string.Empty, switchDocActivo.IsToggled);

        var resultado = _documentoExistente is null
            ? await ApiClient.PostAsync<DocumentoContextoDto>("/api/admin/asistente/documentos", body)
            : await ApiClient.PutAsync<DocumentoContextoDto>($"/api/admin/asistente/documentos/{_documentoExistente.Id}", body);

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo guardar el documento.", "OK");
            return;
        }

        modalDocumento.IsVisible = false;
        await _viewModel.CargarAsync();
    }

    private async void OnEliminarDocumentoClicked(object sender, EventArgs e)
    {
        if (_documentoExistente is null)
            return;

        var confirmar = await DisplayAlert("Eliminar documento", $"¿Borrar \"{_documentoExistente.Nombre}\"?", "Sí", "No");
        if (!confirmar)
            return;

        var resultado = await ApiClient.DeleteAsync($"/api/admin/asistente/documentos/{_documentoExistente.Id}");
        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar.", "OK");
            return;
        }

        modalDocumento.IsVisible = false;
        await _viewModel.CargarAsync();
    }

    // --- prompts --------------------------------------------------------------------------------

    private async void OnGuardarPromptClicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: PromptRow fila })
            return;

        if (string.IsNullOrWhiteSpace(fila.Contenido))
        {
            await DisplayAlert("Error", "El contenido no puede quedar vacío.", "OK");
            return;
        }

        var resultado = await ApiClient.PutAsync<PromptIADto>(
            $"/api/admin/asistente/prompts/{fila.Clave}",
            new PromptIARequestDto(fila.Contenido));

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo guardar el prompt.", "OK");
            return;
        }

        await DisplayAlert("Listo", $"\"{fila.Titulo}\" se guardó.", "OK");
    }
}
