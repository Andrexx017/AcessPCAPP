namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// La versión de escritorio: mapa y test en dos paneles, visibles a la vez. No hay pantalla de
/// bifurcación ni secuencia de una pregunta por vez — en una pantalla grande caben las preguntas de
/// la zona completas, y verlas de un golpe es más rápido. El flujo de tres pantallas
/// (<see cref="MapaCapacidadesView"/>) existe porque en un teléfono eso no cabe.
///
/// Comparte el <see cref="MapaCapacidadesViewModel"/> con la versión móvil: el estado del recorrido
/// y las reglas de qué cuenta como zona resuelta son los mismos; solo cambia el layout y el flujo.
/// </summary>
public partial class MapaCapacidadesEscritorioView : ContentPage
{
    // Mismo mecanismo que Features/Auth/LoginView.xaml.cs y Features/Historial/HistorialView.xaml.cs:
    // MAUI no tiene media queries, así que el paso de dos paneles a uno encima de otro se hace a
    // mano según el ancho disponible.
    private const double AnchoDosPaneles = 1000;

    private readonly MapaCapacidadesViewModel _vm;
    private ZonaPanelViewModel? _panel;
    private bool? _dosPaneles;

    public MapaCapacidadesEscritorioView()
    {
        InitializeComponent();
        _vm = new MapaCapacidadesViewModel();
        BindingContext = _vm;
    }

    private bool _catalogoCargado;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // A diferencia de la versión móvil, acá no se vuelve a esta pantalla desde una zona (el
        // panel vive al lado, no se navega), así que basta con cargar el catálogo una vez.
        if (_catalogoCargado)
            return;

        _catalogoCargado = true;
        await _vm.CargarYRefrescarAsync();

        if (_vm.UltimoError is not null)
            await DisplayAlert("Error", _vm.UltimoError, "OK");
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0)
            return;

        var dos = width >= AnchoDosPaneles;
        if (_dosPaneles == dos)
            return;

        _dosPaneles = dos;
        Acomodar(dos);
    }

    private void Acomodar(bool dosPaneles)
    {
        Paneles.ColumnDefinitions.Clear();
        Paneles.RowDefinitions.Clear();

        if (dosPaneles)
        {
            Paneles.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(430)));
            Paneles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Paneles.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid.SetColumn(PanelMapa, 0);
            Grid.SetRow(PanelMapa, 0);
            Grid.SetColumn(PanelZona, 1);
            Grid.SetRow(PanelZona, 0);
        }
        else
        {
            // Ventana angosta: el mapa arriba y la zona debajo, cada uno a su alto natural.
            Paneles.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Paneles.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Paneles.RowDefinitions.Add(new RowDefinition(GridLength.Star));

            Grid.SetColumn(PanelMapa, 0);
            Grid.SetRow(PanelMapa, 0);
            Grid.SetColumn(PanelZona, 0);
            Grid.SetRow(PanelZona, 1);
        }
    }

    private void OnZonaTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: ZonaMapaItem item })
            return;

        AbrirZona(item.Zona);
    }

    private void AbrirZona(ZonaCorporal zona)
    {
        _panel = new ZonaPanelViewModel(zona, _vm);
        ContenidoZona.BindingContext = _panel;
        ContenidoZona.IsVisible = true;
        VacioZona.IsVisible = false;

        // Resalta la zona abierta en el mapa: acá los dos paneles se ven juntos y hay que saber
        // cuál se está editando.
        _vm.Seleccionar(zona);
    }

    private void OnOpcionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: OpcionItem item })
            item.AlElegir?.Invoke();
    }

    private void OnSinNovedadClicked(object sender, EventArgs e)
    {
        _panel?.MarcarSinNovedad();
    }

    private void OnSiguienteZonaClicked(object sender, EventArgs e)
    {
        if (_panel is null)
            return;

        var siguiente = _vm.SiguienteDespuesDe(_panel.Zona) ?? CatalogoZonas.Todas[0];
        AbrirZona(siguiente);
    }

    private async void OnDictamenClicked(object sender, EventArgs e)
    {
        // Antes de pedir el dictamen, si queda alguna pregunta activa sin responder se salta
        // directo a esa zona en vez de pedirle al enfermero que recorra las 10 buscando cuál falta.
        var pendiente = _vm.PrimeraZonaPendiente;
        if (pendiente is not null)
        {
            AbrirZona(pendiente);
            var faltantes = _vm.PreguntasPendientes(pendiente);
            await DisplayAlert(
                "Faltan respuestas",
                $"\"{pendiente.Nombre}\" tiene {faltantes.Count} pregunta(s) sin responder:\n\n{string.Join("\n", faltantes)}",
                "OK");
            return;
        }

        // El dictamen ya no se pide aquí: la pantalla de Resumen lo pide cuando el personal
        // confirma con OK, después de revisar las respuestas y de preguntarle a la IA si hace falta.
        await Navigation.PushAsync(new ResumenDiagnosticoEscritorioView(_vm));
    }
}
