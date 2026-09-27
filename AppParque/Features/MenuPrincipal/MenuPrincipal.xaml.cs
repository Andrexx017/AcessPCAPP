using Microsoft.Maui.Controls;
using System;
using AppParque.Features.RegistroVisitante;
using AppParque.Features.Historial;

namespace AppParque.Features.MenuPrincipal
{
    public partial class MenuPrincipal : ContentPage
    {
        public MenuPrincipal()
        {
            InitializeComponent();
        }


        private async Task OnCerrarSesionClicked(object sender, EventArgs e)
        {
            bool confirmacion = await DisplayAlert("Cerrar Sesi�n", "�Deseas cerrar sesi�n?", "S�", "No");

            if (confirmacion)
            {
                // Regresar al Login y limpiar historial de navegaci�n
                await Navigation.PopToRootAsync();
            }
        }


        private async void OnHamburguesaClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(
                $"Hola, Bienvenid@",
                "Cancelar",
                null,
                "Ver Perfil",
                "Cerrar Sesi�n"
            );

            if (action == "Ver Perfil")
            {
                await DisplayAlert("Perfil", $"Nombre: \nCorreo: ", "Cerrar");
            }
            else if (action == "Cerrar Sesi�n")
            {
                await OnCerrarSesionClicked(sender, e);
            }
        }

        private async void OnIniciarTestClicked(object sender, EventArgs e)
        {
            // Redirigir a la pantalla del Test
            await Navigation.PushAsync(new RegisterDataView());
        }

        private async void OnHistorialClicked(object sender, EventArgs e)
        {
            // Redirigir al historial
            await Navigation.PushAsync(new HistorialView());
        }
    }
}
