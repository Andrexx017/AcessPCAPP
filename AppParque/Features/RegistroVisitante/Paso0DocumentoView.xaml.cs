using System.Net;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.RegistroVisitante;

public partial class Paso0DocumentoView : ContentPage
{
    public Paso0DocumentoView()
    {
        InitializeComponent();
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        var tipoDocumento = pickerTipoId.SelectedItem?.ToString();
        var numeroDocumento = entryNumeroId.Text?.Trim();

        if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numeroDocumento))
        {
            await DisplayAlert("Error", "Selecciona el tipo e ingresa el número de documento.", "OK");
            return;
        }

        btnBuscar.IsEnabled = false;

        var tipoQs = Uri.EscapeDataString(tipoDocumento);
        var numeroQs = Uri.EscapeDataString(numeroDocumento);
        var busqueda = await ApiClient.GetAsync<VisitanteDto>($"/api/visitantes/buscar?tipoDocumento={tipoQs}&numeroDocumento={numeroQs}");

        btnBuscar.IsEnabled = true;

        if (busqueda.Success)
        {
            UsuarioGlobal.VisitanteId = busqueda.Data.Id;
            UsuarioGlobal.Name = busqueda.Data.Nombre;
            UsuarioGlobal.TypeId = busqueda.Data.TipoDocumento;
            UsuarioGlobal.IdNumber = busqueda.Data.NumeroDocumento;

            await Navigation.PushAsync(new ConfirmacionEvaluacionView(busqueda.Data));
            return;
        }

        if (busqueda.StatusCode == HttpStatusCode.NotFound)
        {
            await Navigation.PushAsync(new RegistroCompletoView(tipoDocumento, numeroDocumento));
            return;
        }

        await DisplayAlert("Error", busqueda.ErrorMessage ?? "No se pudo consultar el visitante.", "OK");
    }

    private async void OnOmitirClicked(object sender, EventArgs e)
    {
        var tipoDocumento = pickerTipoId.SelectedItem?.ToString();
        var numeroDocumento = entryNumeroId.Text?.Trim();

        await Navigation.PushAsync(new RegistroCompletoView(tipoDocumento, numeroDocumento));
    }
}
