namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Pantalla 1 del flujo móvil: el mapa del cuerpo como índice. Al tocar una zona se abre su puerta
/// (<see cref="ZonaPuertaView"/>) a pantalla completa; el mapa no se encoge, se va. Al terminar la
/// zona se vuelve aquí para seguir con otra parte del cuerpo.
///
/// El ViewModel se crea aquí y se pasa por constructor a las demás pantallas: es el estado del
/// recorrido completo y tiene que sobrevivir a las idas y vueltas.
/// </summary>
public partial class MapaCapacidadesView : ContentPage
{
    private readonly MapaCapacidadesViewModel _vm;

    public MapaCapacidadesView()
    {
        InitializeComponent();
        _vm = new MapaCapacidadesViewModel();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // La primera vez trae el catálogo de la API (CatalogoZonas.CargarAsync es idempotente, así
        // que volver de una zona solo repinta sin pegarle de nuevo al servidor). Al volver de una
        // zona hay que repintar: cambió su estado y los contadores.
        await _vm.CargarYRefrescarAsync();

        if (_vm.UltimoError is not null)
            await DisplayAlert("Error", _vm.UltimoError, "OK");
    }

    private async void OnZonaTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: ZonaMapaItem item })
            return;

        await Navigation.PushAsync(new ZonaPuertaView(item.Zona, _vm));
    }

    private async void OnPieClicked(object sender, EventArgs e)
    {
        if (_vm.TodoResuelto)
        {
            await MostrarResumenAsync();
            return;
        }

        var zona = _vm.PrimeraSinRevisar;
        if (zona is null)
        {
            // No queda ninguna sin tocar pero alguna está a medias: no hay a dónde "empezar",
            // así que se pasa al resumen y el motor marcará lo que falte como por verificar.
            await MostrarResumenAsync();
            return;
        }

        await Navigation.PushAsync(new ZonaPuertaView(zona, _vm));
    }

    private async Task MostrarResumenAsync()
    {
        // El dictamen ya no se pide aquí: la pantalla de Resumen lo pide cuando el personal
        // confirma con OK, después de revisar las respuestas y de preguntarle a la IA si hace falta.
        await Navigation.PushAsync(new ResumenDiagnosticoView(_vm));
    }
}
