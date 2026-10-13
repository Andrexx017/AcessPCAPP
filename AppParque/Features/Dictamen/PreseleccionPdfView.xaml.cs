using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

public partial class PreseleccionPdfView : ContentPage
{
    private readonly PreseleccionPdfViewModel _vm;

    public PreseleccionPdfView(DiagnosticoResponseDto dictamen)
    {
        InitializeComponent();
        _vm = new PreseleccionPdfViewModel(dictamen);
        BindingContext = _vm;
    }

    private async void OnAtrasClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnGenerarClicked(object sender, EventArgs e)
    {
        var seleccionadas = _vm.ObtenerSeleccionadas();
        await Navigation.PushAsync(new VistaPreviaPdfView(seleccionadas));
    }
}
