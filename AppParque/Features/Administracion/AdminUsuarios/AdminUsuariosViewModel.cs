using System.Collections.ObjectModel;
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
                Usuarios.Add(new UsuarioRow(u, OnToggledAsync));
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

    public UsuarioRow(UsuarioAdminDto model, Func<UsuarioRow, Task> onToggled)
    {
        Model = model;
        _activo = model.Activo;
        _onToggled = onToggled;
    }

    public void RevertActivoSinDisparar(bool valor)
    {
        _activo = valor;
        OnPropertyChanged(nameof(Activo));
    }
}
