using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;
using AppParque.Features.EvaluacionAccesibilidad;

namespace AppParque.Features.RegistroVisitante;

public partial class RegisterDataView : ContentPage
{
	public RegisterDataView()
	{
		InitializeComponent();
	}


    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var visitante = new DataUser
        {
            name = entryNombre.Text,
            typeId = pickerTipoId.SelectedItem?.ToString(),
            idNumber = entryNumeroId.Text,
            age = entryEdad.Text,
            stature = entryEstatura.Text
        };

        // Validar que ning�n campo est� vac�o
        if (string.IsNullOrWhiteSpace(visitante.name) ||
            string.IsNullOrWhiteSpace(visitante.typeId) ||
            string.IsNullOrWhiteSpace(visitante.idNumber) ||
            string.IsNullOrWhiteSpace(visitante.age) ||
            string.IsNullOrWhiteSpace(visitante.stature))
        {
            await DisplayAlert("Error", "Llene todos los campos requeridos.", "OK");
            return;
        }

        // ?? Guardar en la sesi�n global
        UsuarioGlobal.Name = visitante.name;
        UsuarioGlobal.TypeId = visitante.typeId;
        UsuarioGlobal.IdNumber = visitante.idNumber;
        UsuarioGlobal.Age = visitante.age;
        UsuarioGlobal.Stature = visitante.stature;

        // Guardar en Firebase
        var firebase = new FireBaseService();
        bool registrado = await firebase.AddVisitorAsync(visitante);

        if (registrado)
        {
            await DisplayAlert("�xito", "Visitante registrado correctamente.", "OK");
            await Navigation.PushAsync(new TestView());
        }
        else
        {
            await DisplayAlert("Error", "No se pudo registrar el visitante.", "OK");
        }
    }


}