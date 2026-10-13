using System.Globalization;
using AppParque.Features.Administracion.AdminAsistente;
using AppParque.Features.Administracion.AdminAtracciones;
using AppParque.Features.Administracion.AdminUsuarios;
using AppParque.Features.Administracion.AdminZonas;
using AppParque.Features.Historial;
using AppParque.Features.Perfil;
using AppParque.Features.RegistroVisitante;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.Inicio;

public partial class InicioAdminView : ContentPage
{
    // Por debajo de esto la barra lateral se oculta sola para no apretar el contenido;
    // el botón de hamburguesa sigue funcionando para abrirla manualmente en cualquier ancho.
    private const double SidebarBreakpoint = 860;

    // Por debajo de esto el saludo + los botones del topbar (campana, tema, avatar, "+ Nueva
    // evaluación") no caben en una sola fila y se apilan en dos. Es más chico que el de la barra
    // lateral porque el topbar solo necesita caber él mismo, no dejar espacio también para la
    // barra lateral.
    private const double TopbarBreakpoint = 600;

    private readonly InicioAdminViewModel _viewModel = new();
    private bool _sidebarAbierta = true;
    private bool? _esAnchoAmplio;
    private bool? _topbarApilado;

    public InicioAdminView()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        bool esAnchoAmplio = width >= SidebarBreakpoint;
        if (_esAnchoAmplio != esAnchoAmplio)
        {
            _esAnchoAmplio = esAnchoAmplio;
            _sidebarAbierta = esAnchoAmplio;
            sidebar.IsVisible = esAnchoAmplio;
        }

        bool apilar = width < TopbarBreakpoint;
        if (_topbarApilado == apilar)
            return;

        _topbarApilado = apilar;
        topbarGrid.RowDefinitions.Clear();
        topbarGrid.ColumnDefinitions.Clear();

        if (apilar)
        {
            topbarGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            topbarGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            topbarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetRow(topbarGreeting, 0);
            Grid.SetColumn(topbarGreeting, 0);
            Grid.SetRow(topbarActions, 1);
            Grid.SetColumn(topbarActions, 0);
        }
        else
        {
            topbarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            topbarGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetRow(topbarGreeting, 0);
            Grid.SetColumn(topbarGreeting, 0);
            Grid.SetRow(topbarActions, 0);
            Grid.SetColumn(topbarActions, 1);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string? nombre = UsuarioGlobal.nurseName;
        string saludoBase = SaludoSegunHora(DateTime.Now.Hour);
        lblSaludo.Text = string.IsNullOrWhiteSpace(nombre) ? saludoBase : $"{saludoBase}, {PrimerNombre(nombre)}";
        lblFecha.Text = CultureInfo.GetCultureInfo("es-CO").TextInfo.ToTitleCase(DateTime.Today.ToString("dddd d 'de' MMMM", CultureInfo.GetCultureInfo("es-CO")));
        lblIniciales.Text = CalcularIniciales(nombre);

        sideMenu.HeaderText = lblSaludo.Text;
        sideMenu.SetAvatar(null, nombre);

        _ = _viewModel.CargarAsync();
    }

    private static string SaludoSegunHora(int hora) => hora switch
    {
        >= 5 and < 12 => "Buenos días",
        >= 12 and < 19 => "Buenas tardes",
        _ => "Buenas noches",
    };

    private static string PrimerNombre(string nombreCompleto) =>
        nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombreCompleto;

    private static string CalcularIniciales(string? nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return "?";

        var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var iniciales = string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        return string.IsNullOrEmpty(iniciales) ? "?" : iniciales;
    }

    // ====== Sidebar: se abre/oculta con el botón de hamburguesa (y sola en ventanas angostas) ======
    private void OnToggleSidebarTapped(object? sender, TappedEventArgs e)
    {
        _sidebarAbierta = !_sidebarAbierta;
        sidebar.IsVisible = _sidebarAbierta;
    }

    private async void OnNotificacionesTapped(object? sender, TappedEventArgs e)
    {
        await DisplayAlert("Notificaciones", "Próximamente...", "OK");
    }

    private async void OnMapaTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new MapaZoomView());
    }

    // ====== Avatar: abre el mismo panel de perfil/cerrar sesión que el resto de la app ======
    private void OnAvatarTapped(object? sender, TappedEventArgs e)
    {
        _ = sideMenu.ToggleAsync();
    }

    private async void OnPerfilTapped(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new PerfilView());
    }

    private async void OnCerrarSesionTapped(object? sender, EventArgs e)
    {
        bool confirmacion = await DisplayAlert("Cerrar Sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (!confirmacion)
            return;

        if (!string.IsNullOrEmpty(ApiClient.RefreshToken))
            await ApiClient.PostAsync("/api/auth/logout", new { refreshToken = ApiClient.RefreshToken });

        ApiClient.ClearSession();
        UsuarioGlobal.UsuarioId = 0;
        UsuarioGlobal.Role = null;
        UsuarioGlobal.nurseName = null;
        UsuarioGlobal.VisitanteId = 0;

        await Navigation.PopToRootAsync();
    }

    // ====== Navegación de módulos (idéntica a MenuAdmin) ======
    private async void OnNuevaEvaluacionClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Paso0DocumentoView());
    }

    private async void OnRealizarTestTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new Paso0DocumentoView());
    }

    private async void OnHistorialTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new HistorialView());
    }

    private async void OnCatalogoPreguntasTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AdminZonasView());
    }

    private async void OnCatalogoAtraccionesTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AdminAtraccionesView());
    }

    private async void OnUsuariosTapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AdminUsuariosView());
    }

    private async void OnAsistenteIATapped(object? sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AdminAsistenteView());
    }
}
