using AppParque.Shared.Models;

namespace AppParque.Features.ValidacionAtracciones;

public partial class ValidacionAtraccionesView : ContentPage
{
    public ValidacionAtraccionesView(Restrictions restricciones, int estatura)
    {
        InitializeComponent();
        BindingContext = new ValidacionAtraccionesViewModel(restricciones, estatura);
    }
}
