namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Pantalla 3 del flujo móvil: la secuencia guiada de la zona, a pantalla completa. Al contestar la
/// última pregunta se vuelve al mapa con la zona marcada, listo para la siguiente parte del cuerpo.
/// </summary>
public partial class ZonaSecuenciaView : ContentPage
{
    private readonly ZonaSecuenciaViewModel _vm;

    public ZonaSecuenciaView(ZonaCorporal zona, MapaCapacidadesViewModel mapa)
    {
        InitializeComponent();
        _vm = new ZonaSecuenciaViewModel(zona, mapa);
        BindingContext = _vm;
    }

    private void OnOpcionTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: OpcionItem item })
            item.AlElegir?.Invoke();
    }

    private async void OnSiguienteClicked(object sender, EventArgs e)
    {
        if (_vm.Avanzar())
            await Navigation.PopAsync();
    }

    private async void OnAtrasClicked(object sender, EventArgs e)
    {
        if (_vm.Atras())
            await Navigation.PopAsync();
    }

    /// <summary>
    /// «No sabe o no responde» avanza sin guardar: la dimensión queda sin dato y el motor la
    /// devolverá como «por verificar», en vez de inventar un sí o un no. La zona queda a medias
    /// en el mapa, que es la verdad.
    /// </summary>
    private async void OnSaltarClicked(object sender, EventArgs e)
    {
        if (_vm.Avanzar())
            await Navigation.PopAsync();
    }
}
