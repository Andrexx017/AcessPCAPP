using AppParque.Features.Administracion.AdminAtracciones;
using AppParque.Features.Administracion.AdminPreguntas;
using AppParque.Features.Administracion.AdminUsuarios;
using AppParque.Features.Historial;
using AppParque.Features.Perfil;
using AppParque.Features.RegistroVisitante;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion;

public partial class MenuAdmin : ContentPage
{
    public MenuAdmin()
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

    private async Task OnCerrarSesionClicked()
    {
        bool confirmacion = await DisplayAlert("Cerrar Sesión", "¿Deseas cerrar sesión?", "Sí", "No");
        if (!confirmacion)
            return;

        if (!string.IsNullOrEmpty(ApiClient.RefreshToken))
            await ApiClient.PostAsync("/api/auth/logout", new { refreshToken = ApiClient.RefreshToken });

        ApiClient.ClearSession();
        UsuarioGlobal.UsuarioId = 0;
        UsuarioGlobal.Role = null;
        UsuarioGlobal.nurseName = null;
        UsuarioGlobal.VisitanteId = 0;

        await Navigation.PopToRootAsync();
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
        await OnCerrarSesionClicked();
    }

    private async void OnRealizarTestClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterDataView());
    }

    private async void OnHistorialClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HistorialView());
    }

    private async void OnCatalogosClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet(
            "Catálogos",
            "Cancelar",
            null,
            "Preguntas (RF-09)",
            "Atracciones (RF-10)");

        if (action == "Preguntas (RF-09)")
            await Navigation.PushAsync(new AdminPreguntasView());
        else if (action == "Atracciones (RF-10)")
            await Navigation.PushAsync(new AdminAtraccionesView());
    }

    private async void OnUsuariosClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AdminUsuariosView());
    }
}
