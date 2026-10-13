using AppParque.Features.Dictamen;

namespace AppParque.Features.EvaluacionAccesibilidad;

public partial class ResumenDiagnosticoEscritorioView : ContentPage
{
    private readonly ResumenDiagnosticoViewModel _vm;

    public ResumenDiagnosticoEscritorioView(MapaCapacidadesViewModel mapaVm)
    {
        InitializeComponent();
        _vm = new ResumenDiagnosticoViewModel(mapaVm);
        BindingContext = _vm;
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnPreguntarIaClicked(object sender, EventArgs e)
    {
        var ok = await _vm.PreguntarIaAsync();
        if (!ok)
            await DisplayAlert("No se pudo preguntar", _vm.UltimoError ?? "Intenta de nuevo.", "OK");
    }

    private async void OnOkClicked(object sender, EventArgs e)
    {
        var dictamen = await _vm.PedirDictamenAsync();
        if (dictamen is null)
        {
            await DisplayAlert("No se pudo calcular el dictamen", _vm.UltimoErrorDictamen ?? "Intenta de nuevo.", "OK");
            return;
        }

        await Navigation.PushAsync(new DictamenView(
            dictamen,
            _vm.UltimoPerfil,
            _vm.Secciones,
            _vm.CondicionAdicional,
            _vm.RespuestaIA,
            _vm.RespuestaIAModelo,
            _vm.RespuestaIASesionId));
    }
}
