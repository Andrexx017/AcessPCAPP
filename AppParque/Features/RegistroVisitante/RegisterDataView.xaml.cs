using System.Net;
using AppParque.Features.EvaluacionAccesibilidad;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.RegistroVisitante;

public partial class RegisterDataView : ContentPage
{
    public RegisterDataView()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var nombre = entryNombre.Text?.Trim();
        var tipoDocumento = pickerTipoId.SelectedItem?.ToString();
        var numeroDocumento = entryNumeroId.Text?.Trim();
        var edad = entryEdad.Text?.Trim();
        var estatura = entryEstatura.Text?.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(tipoDocumento) ||
            string.IsNullOrWhiteSpace(numeroDocumento) || string.IsNullOrWhiteSpace(edad) ||
            string.IsNullOrWhiteSpace(estatura))
        {
            await DisplayAlert("Error", "Llene todos los campos requeridos.", "OK");
            return;
        }

        if (!checkboxConsentimiento.IsChecked)
        {
            await DisplayAlert("Autorización requerida", "Para continuar, el visitante (o su acudiente) debe autorizar el tratamiento de sus datos personales y de salud.", "OK");
            return;
        }

        var tipoQs = Uri.EscapeDataString(tipoDocumento);
        var numeroQs = Uri.EscapeDataString(numeroDocumento);

        var busqueda = await ApiClient.GetAsync<VisitanteDto>($"/api/visitantes/buscar?tipoDocumento={tipoQs}&numeroDocumento={numeroQs}");

        VisitanteDto? visitante = null;

        if (busqueda.Success)
        {
            visitante = busqueda.Data;
            await DisplayAlert("Visitante encontrado", $"Ya existe un registro para {visitante.Nombre}. Se usará su ficha existente.", "OK");
        }
        else if (busqueda.StatusCode == HttpStatusCode.NotFound)
        {
            var creacion = await ApiClient.PostAsync<VisitanteDto>("/api/visitantes", new
            {
                tipoDocumento,
                numeroDocumento,
                nombre,
                consentimientoTratamientoDatos = true,
            });

            if (!creacion.Success)
            {
                await DisplayAlert("Error", creacion.ErrorMessage ?? "No se pudo registrar el visitante.", "OK");
                return;
            }

            visitante = creacion.Data;
        }
        else
        {
            await DisplayAlert("Error", busqueda.ErrorMessage ?? "No se pudo consultar el visitante.", "OK");
            return;
        }

        UsuarioGlobal.VisitanteId = visitante.Id;
        UsuarioGlobal.Name = visitante.Nombre;
        UsuarioGlobal.TypeId = tipoDocumento;
        UsuarioGlobal.IdNumber = numeroDocumento;
        UsuarioGlobal.Age = edad;
        UsuarioGlobal.Stature = estatura;

        await Navigation.PushAsync(new TestView());
    }
}
