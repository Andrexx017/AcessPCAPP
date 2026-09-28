using AppParque.Services;

namespace AppParque.Features.Administracion.AdminUsuarios;

public partial class AdminUsuarioEditView : ContentPage
{
    private readonly UsuarioAdminDto? _usuarioExistente;

    public AdminUsuarioEditView(UsuarioAdminDto? usuarioExistente)
    {
        InitializeComponent();
        _usuarioExistente = usuarioExistente;

        if (_usuarioExistente is null)
        {
            Title = "Nuevo usuario";
            stackActivo.IsVisible = false;
        }
        else
        {
            Title = "Editar usuario";
            entryNombre.Text = _usuarioExistente.NombreCompleto;
            entryUsername.Text = _usuarioExistente.Username;
            entryUsername.IsEnabled = false;
            pickerRol.SelectedItem = _usuarioExistente.Rol;
            switchActivo.IsToggled = _usuarioExistente.Activo;

            // El username no se puede cambiar (es la clave de login); la contraseña se cambia aparte.
            stackPassword.IsVisible = false;
            labelPassword.Text = "";
            ToolbarItems.Add(new ToolbarItem("Cambiar contraseña", null, () => _ = CambiarPasswordAsync()));
        }
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var nombre = entryNombre.Text?.Trim();
        var rol = pickerRol.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(rol))
        {
            await DisplayAlert("Error", "Llene el nombre y seleccione un rol.", "OK");
            return;
        }

        if (_usuarioExistente is null)
        {
            var username = entryUsername.Text?.Trim();
            var password = entryPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Llene el username y la contraseña.", "OK");
                return;
            }

            var resultado = await ApiClient.PostAsync<UsuarioAdminDto>("/api/admin/usuarios", new
            {
                nombreCompleto = nombre,
                username,
                password,
                rol,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo crear el usuario.", "OK");
                return;
            }
        }
        else
        {
            var resultado = await ApiClient.PutAsync<UsuarioAdminDto>($"/api/admin/usuarios/{_usuarioExistente.Id}", new
            {
                nombreCompleto = nombre,
                rol,
                activo = switchActivo.IsToggled,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo actualizar el usuario.", "OK");
                return;
            }
        }

        await Navigation.PopAsync();
    }

    private async Task CambiarPasswordAsync()
    {
        if (_usuarioExistente is null)
            return;

        var nuevaPassword = await DisplayPromptAsync("Cambiar contraseña", $"Nueva contraseña para {_usuarioExistente.Username}:");
        if (string.IsNullOrWhiteSpace(nuevaPassword))
            return;

        var resultado = await ApiClient.PutAsync<object?>($"/api/admin/usuarios/{_usuarioExistente.Id}/password", new { nuevoPassword = nuevaPassword });

        await DisplayAlert(
            resultado.Success ? "Listo" : "Error",
            resultado.Success ? "Contraseña actualizada." : resultado.ErrorMessage ?? "No se pudo cambiar la contraseña.",
            "OK");
    }
}
