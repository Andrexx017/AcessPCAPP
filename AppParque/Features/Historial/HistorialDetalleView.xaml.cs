namespace AppParque.Features.Historial;

public partial class HistorialDetalleView : ContentPage
{
    public HistorialDetalleView(int diagnosticoId, string visitorNameFallback, string visitorIdFallback)
    {
        InitializeComponent();
        BindingContext = new HistorialDetalleViewModel(diagnosticoId, visitorNameFallback, visitorIdFallback);
    }
}
