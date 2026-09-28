using AppParque.Shared.Models;

namespace AppParque.Features.ValidacionAtracciones;

public partial class ValidacionAtraccionesView : ContentPage
{
    public ValidacionAtraccionesView(EvaluacionResponseDto evaluacion)
    {
        InitializeComponent();
        BindingContext = new ValidacionAtraccionesViewModel(evaluacion);
    }
}
