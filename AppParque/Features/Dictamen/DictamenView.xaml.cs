using AppParque.Features.EvaluacionAccesibilidad;
using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

/// <summary>
/// El resultado del motor de capacidades: un veredicto por atracción con los motivos que lo
/// produjeron (la preselección), más el diagnóstico resumido y la respuesta de la IA que vienen
/// de la pantalla de Resumen, y el botón para confirmar y guardarlo todo en el historial.
/// </summary>
public partial class DictamenView : ContentPage
{
    private readonly DictamenViewModel _vm;

    public DictamenView(
        DiagnosticoResponseDto dictamen,
        PerfilCapacidadesDto? perfil,
        IReadOnlyList<ResumenZonaItem> diagnosticoSecciones,
        string? condicionAdicional,
        string? respuestaIA = null,
        string? respuestaIAModelo = null,
        Guid? respuestaIASesionId = null)
    {
        InitializeComponent();
        _vm = new DictamenViewModel(dictamen, perfil, diagnosticoSecciones, condicionAdicional, respuestaIA, respuestaIAModelo, respuestaIASesionId);
        BindingContext = _vm;
    }

    private void OnFiltroTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: FiltroItem item })
            _vm.Filtrar(item.Clave);
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnGenerarGuiaClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new PreseleccionPdfView(_vm.Dictamen));
    }

    private async void OnConfirmarClicked(object sender, EventArgs e)
    {
        var ok = await _vm.ConfirmarAsync();

        if (!ok)
        {
            await DisplayAlert("No se pudo guardar", _vm.UltimoErrorGuardado ?? "Intenta de nuevo.", "OK");
            return;
        }

        await DisplayAlert("Guardado", "El diagnóstico quedó guardado en el historial.", "OK");
        await VolverAlMenuAsync();
    }

    /// <summary>
    /// PopToRootAsync NO sirve aquí: en esta app la raíz de la pila de navegación es LoginView
    /// (MenuPrincipal.OnCerrarSesionClicked usa ese mismo método para cerrar sesión), así que
    /// volver "a la raíz" tras confirmar un diagnóstico terminaría deslogueando al enfermero. En
    /// vez de eso se quita todo lo que hay entre el Menú Principal y esta pantalla (mismo patrón
    /// que ZonaPuertaView.RemovePage) y se hace un solo PopAsync animado hasta el menú.
    /// </summary>
    private async Task VolverAlMenuAsync()
    {
        var pila = Navigation.NavigationStack;
        var indiceMenu = -1;
        for (var i = 0; i < pila.Count; i++)
        {
            if (pila[i] is AppParque.Features.MenuPrincipal.MenuPrincipal)
            {
                indiceMenu = i;
                break;
            }
        }

        if (indiceMenu < 0)
        {
            await Navigation.PopToRootAsync();
            return;
        }

        for (var i = pila.Count - 2; i > indiceMenu; i--)
            Navigation.RemovePage(pila[i]);

        await Navigation.PopAsync();
    }
}
