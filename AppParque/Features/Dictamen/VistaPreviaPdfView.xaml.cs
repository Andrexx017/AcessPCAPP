using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

public partial class VistaPreviaPdfView : ContentPage
{
    private readonly VistaPreviaPdfViewModel _vm;

    public VistaPreviaPdfView(List<AtraccionPdfInfo> seleccionadas)
    {
        InitializeComponent();
        _vm = new VistaPreviaPdfViewModel(seleccionadas);
        BindingContext = _vm;
    }

    private async void OnDescargarClicked(object sender, EventArgs e)
    {
        await _vm.DescargarAsync();
    }

    private async void OnCompartirClicked(object sender, EventArgs e)
    {
        await _vm.CompartirAsync();
    }
}
