using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Input;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminUsuarios;

public class AdminUsuariosViewModel : BaseViewModel
{
    public ObservableCollection<UsuarioRow> Usuarios { get; } = new();

    public AdminUsuariosViewModel()
    {
        _ = CargarAsync();
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var resultado = await ApiClient.GetAsync<List<UsuarioAdminDto>>("/api/admin/usuarios");

        Usuarios.Clear();
        if (resultado.Success)
        {
            foreach (var u in resultado.Data)
                Usuarios.Add(new UsuarioRow(u, OnToggledAsync, OnCambiarPasswordAsync, OnEliminarAsync));
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudieron cargar los usuarios.", "OK");
        }

        IsBusy = false;
    }

    private async Task OnToggledAsync(UsuarioRow row)
    {
        var resultado = await ApiClient.PutAsync<UsuarioAdminDto>(
            $"/api/admin/usuarios/{row.Model.Id}",
            new { nombreCompleto = row.Model.NombreCompleto, rol = row.Model.Rol, activo = row.Activo });

        if (!resultado.Success)
        {
            // Revertir el switch visual (p.ej. el backend bloquea auto-desactivarse) y avisar.
            row.RevertActivoSinDisparar(!row.Activo);
            await Application.Current.MainPage.DisplayAlert("Aviso", resultado.ErrorMessage ?? "No se pudo actualizar el usuario.", "OK");
        }
    }

    private async Task OnCambiarPasswordAsync(UsuarioRow row)
    {
        var mainPage = Application.Current.MainPage;

        var nuevaPassword = await mainPage.DisplayPromptAsync("Cambiar contraseña", $"Nueva contraseña para {row.Username}:");
        if (string.IsNullOrWhiteSpace(nuevaPassword))
            return;

        var resultado = await ApiClient.PutAsync<object?>($"/api/admin/usuarios/{row.Model.Id}/password", new { nuevoPassword = nuevaPassword });

        await mainPage.DisplayAlert(
            resultado.Success ? "Listo" : "Error",
            resultado.Success ? "Contraseña actualizada." : resultado.ErrorMessage ?? "No se pudo cambiar la contraseña.",
            "OK");
    }

    private async Task OnEliminarAsync(UsuarioRow row)
    {
        var mainPage = Application.Current.MainPage;

        var confirmar = await mainPage.DisplayAlert("Eliminar usuario", $"¿Eliminar a \"{row.NombreCompleto}\"? Esta acción no se puede deshacer.", "Sí", "No");
        if (!confirmar)
            return;

        // Paso de verificación: se pide la contraseña del admin que está logueado (no la del
        // usuario a eliminar) antes de ejecutar el borrado.
        var password = await mainPage.DisplayPromptAsync("Confirmar eliminación", "Ingresa tu contraseña para continuar:", accept: "Eliminar", cancel: "Cancelar");
        if (string.IsNullOrWhiteSpace(password))
            return;

        var verificacion = await ApiClient.PostAsync("/api/auth/verificar-password", new { password });
        if (!verificacion.Success)
        {
            // "Contraseña incorrecta" solo cuando el servidor de verdad rechazó la contraseña (401);
            // cualquier otro fallo (sin red, 404 por servidor desactualizado, 500, etc.) muestra su
            // propio mensaje para no confundir un problema de conexión con una contraseña mala.
            var mensaje = verificacion.StatusCode == HttpStatusCode.Unauthorized
                ? "Contraseña incorrecta."
                : verificacion.ErrorMessage ?? "No se pudo verificar la contraseña.";
            await mainPage.DisplayAlert("Error", mensaje, "OK");
            return;
        }

        var resultado = await ApiClient.DeleteAsync($"/api/admin/usuarios/{row.Model.Id}");
        if (!resultado.Success)
        {
            await mainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar el usuario.", "OK");
            return;
        }

        Usuarios.Remove(row);
    }
}

public class UsuarioRow : BaseViewModel
{
    private readonly Func<UsuarioRow, Task> _onToggled;
    private bool _activo;

    public UsuarioAdminDto Model { get; }

    public string NombreCompleto => Model.NombreCompleto;
    public string Username => Model.Username;
    public string Rol => Model.Rol;
    public string Iniciales => string.Concat(Model.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(p => char.ToUpperInvariant(p[0])));

    public bool Activo
    {
        get => _activo;
        set
        {
            if (_activo == value) return;
            _activo = value;
            OnPropertyChanged(nameof(Activo));
            _ = _onToggled(this);
        }
    }

    public ICommand CambiarPasswordCommand { get; }
    public ICommand EliminarCommand { get; }

    public UsuarioRow(UsuarioAdminDto model, Func<UsuarioRow, Task> onToggled, Func<UsuarioRow, Task> onCambiarPassword, Func<UsuarioRow, Task> onEliminar)
    {
        Model = model;
        _activo = model.Activo;
        _onToggled = onToggled;
        CambiarPasswordCommand = new Command(() => _ = onCambiarPassword(this));
        EliminarCommand = new Command(() => _ = onEliminar(this));
    }

    public void RevertActivoSinDisparar(bool valor)
    {
        _activo = valor;
        OnPropertyChanged(nameof(Activo));
    }
}
