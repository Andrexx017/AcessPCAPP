using AppParque.Features.Administracion.AdminAtracciones;
using AppParque.Features.Administracion.AdminPreguntas;
using AppParque.Features.Administracion.AdminUsuarios;
using AppParque.Features.Historial;
using AppParque.Features.Perfil;
using AppParque.Features.RegistroVisitante;
using AppParque.Services;

namespace AppParque.Features.Asistente;

public partial class AsistenteChatContentView : ContentView
{
    /// <summary>Mismas claves que AsistenteChatViewModel.DestinosNav — acá solo se resuelve la
    /// Page de verdad para cada una, porque construir una Page es responsabilidad de la vista, no
    /// del ViewModel.</summary>
    private static readonly Dictionary<string, Func<Page>> PaginasNav = new()
    {
        ["historial"] = () => new HistorialView(),
        ["registro_visitante"] = () => new Paso0DocumentoView(),
        ["perfil"] = () => new PerfilView(),
        ["admin_atracciones"] = () => new AdminAtraccionesView(),
        ["admin_preguntas"] = () => new AdminPreguntasView(),
        ["admin_usuarios"] = () => new AdminUsuariosView(),
    };

    private readonly AsistenteChatViewModel _viewModel = new();
    private string? _imagenPendienteBase64;

    public AsistenteChatContentView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        _viewModel.Mensajes.CollectionChanged += (_, _) => _ = DesplazarAbajoAsync();
        _ = CargarAvatarUsuarioAsync();
        Unloaded += (_, _) => _viewModel.Detener();
    }

    private async Task CargarAvatarUsuarioAsync()
    {
        var resultado = await ApiClient.GetAsync<PerfilDto>("/api/perfil");
        if (!resultado.Success)
            return;

        _viewModel.FotoUsuarioBase64 = resultado.Data.FotoBase64;
        _viewModel.InicialesUsuario = CalcularIniciales(resultado.Data.NombreCompleto);
    }

    private static string CalcularIniciales(string? nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return "?";

        var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => $"{partes[0][..1]}{partes[^1][..1]}".ToUpperInvariant(),
        };
    }

    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        var texto = entryMensaje.Text;
        if (string.IsNullOrWhiteSpace(texto) && _imagenPendienteBase64 is null)
            return;

        entryMensaje.Text = string.Empty;
        var imagen = _imagenPendienteBase64;
        QuitarImagenPendiente();

        await _viewModel.EnviarAsync(texto ?? string.Empty, imagen);
    }

    // --- imagen adjunta -----------------------------------------------------------------------

    private async void OnAdjuntarImagenClicked(object sender, EventArgs e)
    {
        try
        {
            var archivo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Elige una imagen" });
            if (archivo is null)
                return;

            using var stream = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await stream.CopyToAsync(memoria);
            var bytes = memoria.ToArray();

            _imagenPendienteBase64 = Convert.ToBase64String(bytes);
            imgPrevia.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            previaImagen.IsVisible = true;
        }
        catch (Exception ex)
        {
            await (Application.Current?.MainPage?.DisplayAlert("Error", $"No se pudo cargar la imagen: {ex.Message}", "OK") ?? Task.CompletedTask);
        }
    }

    private void OnQuitarImagenClicked(object sender, EventArgs e) => QuitarImagenPendiente();

    private void QuitarImagenPendiente()
    {
        _imagenPendienteBase64 = null;
        imgPrevia.Source = null;
        previaImagen.IsVisible = false;
    }

    // --- menú de temas / cierre de conversación ------------------------------------------------

    private void OnTemaSeleccionado(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: ModoAsistente modo })
            return;

        _viewModel.SeleccionarModo(modo);
    }

    private void OnCambiarTemaClicked(object sender, EventArgs e) => _viewModel.CambiarTema();

    private async void OnListoTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: MensajeChatItem })
            return;

        await _viewModel.CerrarConversacionAsync();
    }

    private void OnOtraPreguntaTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: MensajeChatItem mensaje })
            return;

        _viewModel.ContinuarConversacion(mensaje);
    }

    private async void OnNavTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: MensajeChatItem { NavClave: { } clave } })
            return;

        if (!PaginasNav.TryGetValue(clave, out var crearPagina))
            return;

        var navegacion = Application.Current?.MainPage?.Navigation;
        if (navegacion is not null)
            await navegacion.PushAsync(crearPagina());
    }

    // --- copiar / compartir ---------------------------------------------------------------------

    private async void OnCopiarClicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: MensajeChatItem mensaje })
            return;

        await Clipboard.SetTextAsync(mensaje.Texto);
    }

    private async void OnCompartirClicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: MensajeChatItem mensaje })
            return;

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = mensaje.Texto,
            Title = "Respuesta del asistente — Parque del Café",
        });
    }

    private async Task DesplazarAbajoAsync()
    {
        // Hay que esperar a que BindableLayout termine de agregar el elemento visual antes de medir
        // dónde está el final; sin este respiro el scroll queda un mensaje atrás.
        await Task.Delay(50);
        await scrollMensajes.ScrollToAsync(listaMensajes, ScrollToPosition.End, animated: true);
    }
}
