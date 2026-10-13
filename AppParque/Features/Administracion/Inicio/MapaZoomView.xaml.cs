namespace AppParque.Features.Administracion.Inicio;

/// <summary>
/// Ventana flotante (modal) para ver el croquis del parque en grande: pensada para usarla como
/// guía con el visitante, con zoom por pellizco (táctil) y botones +/- para mouse/escritorio.
/// </summary>
public partial class MapaZoomView : ContentPage
{
    private const double ZoomMinimo = 1.0;
    private const double ZoomMaximo = 4.0;
    private const double PasoBoton = 0.5;

    private double _zoomActual = 1.0;
    private double _zoomInicioGesto = 1.0;
    private double _xOffset;
    private double _yOffset;

    public MapaZoomView()
    {
        InitializeComponent();
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _zoomInicioGesto = _zoomActual;
            return;
        }

        if (e.Status == GestureStatus.Running)
            AplicarZoom(_zoomInicioGesto * e.Scale);
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (_zoomActual <= ZoomMinimo)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                imgMapa.TranslationX = _xOffset + e.TotalX;
                imgMapa.TranslationY = _yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
                _xOffset = imgMapa.TranslationX;
                _yOffset = imgMapa.TranslationY;
                break;
        }
    }

    private void OnAcercarClicked(object sender, EventArgs e) => AplicarZoom(_zoomActual + PasoBoton);

    private void OnAlejarClicked(object sender, EventArgs e) => AplicarZoom(_zoomActual - PasoBoton);

    private void OnRestablecerClicked(object sender, EventArgs e)
    {
        _xOffset = 0;
        _yOffset = 0;
        imgMapa.TranslationX = 0;
        imgMapa.TranslationY = 0;
        AplicarZoom(ZoomMinimo);
    }

    private void OnDobleTapRestablecer(object? sender, TappedEventArgs e) =>
        OnRestablecerClicked(this, EventArgs.Empty);

    private void AplicarZoom(double nuevoZoom)
    {
        _zoomActual = Math.Clamp(nuevoZoom, ZoomMinimo, ZoomMaximo);
        imgMapa.Scale = _zoomActual;

        if (_zoomActual <= ZoomMinimo)
        {
            _xOffset = 0;
            _yOffset = 0;
            imgMapa.TranslationX = 0;
            imgMapa.TranslationY = 0;
        }
    }

    private async void OnCerrarTapped(object? sender, TappedEventArgs e) => await Navigation.PopModalAsync();
}
