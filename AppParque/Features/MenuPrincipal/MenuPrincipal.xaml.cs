using Microsoft.Maui.Controls;
using System;
using AppParque.Features.RegistroVisitante;
using AppParque.Features.Historial;
using AppParque.Features.Perfil;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.MenuPrincipal
{
    public partial class MenuPrincipal : ContentPage
    {
        public MenuPrincipal()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            string saludo = string.IsNullOrWhiteSpace(UsuarioGlobal.nurseName)
                ? "Hola"
                : $"Hola, {UsuarioGlobal.nurseName}";
            lblSaludo.Text = saludo;
            sideMenu.HeaderText = saludo;
            sideMenu.SetAvatar(null, UsuarioGlobal.nurseName);
            _ = CargarAvatarAsync();
        }

        private async Task CargarAvatarAsync()
        {
            var resultado = await ApiClient.GetAsync<PerfilDto>("/api/perfil");
            if (resultado.Success)
                sideMenu.SetAvatar(resultado.Data.FotoBase64, resultado.Data.NombreCompleto);
        }


        private async Task OnCerrarSesionClicked(object sender, EventArgs e)
        {
            bool confirmacion = await DisplayAlert("Cerrar Sesión", "¿Deseas cerrar sesión?", "Sí", "No");

            if (confirmacion)
            {
                if (!string.IsNullOrEmpty(ApiClient.RefreshToken))
                    await ApiClient.PostAsync("/api/auth/logout", new { refreshToken = ApiClient.RefreshToken });

                ApiClient.ClearSession();
                UsuarioGlobal.UsuarioId = 0;
                UsuarioGlobal.Role = null;
                UsuarioGlobal.nurseName = null;
                UsuarioGlobal.VisitanteId = 0;

                // Regresar al Login y limpiar historial de navegación
                await Navigation.PopToRootAsync();
            }
        }


        private void OnMenuLateralClicked(object sender, EventArgs e)
        {
            _ = sideMenu.ToggleAsync();
        }

        private async void OnPerfilTapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerfilView());
        }

        private async void OnCerrarSesionTapped(object? sender, EventArgs e)
        {
            await OnCerrarSesionClicked(sender!, e);
        }

        private async void OnIniciarTestClicked(object sender, EventArgs e)
        {
            // Redirigir a la pantalla del Test
            await Navigation.PushAsync(new Paso0DocumentoView());
        }

        private async void OnHistorialClicked(object sender, EventArgs e)
        {
            // Redirigir al historial
            await Navigation.PushAsync(new HistorialView());
        }
    }
}
