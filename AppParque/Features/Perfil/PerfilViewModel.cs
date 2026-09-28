using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Perfil;

public class PerfilViewModel : BaseViewModel
{
    private string _nombreCompleto = string.Empty;
    private string _username = string.Empty;
    private string? _email;
    private string? _fotoBase64;
    private string _rol = string.Empty;

    public string NombreCompleto { get => _nombreCompleto; set => SetProperty(ref _nombreCompleto, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string? Email { get => _email; set => SetProperty(ref _email, value); }
    public string? FotoBase64 { get => _fotoBase64; set => SetProperty(ref _fotoBase64, value); }
    public string Rol { get => _rol; set => SetProperty(ref _rol, value); }

    public async Task<bool> CargarAsync()
    {
        IsBusy = true;
        var resultado = await ApiClient.GetAsync<PerfilDto>("/api/perfil");
        IsBusy = false;

        if (!resultado.Success)
            return false;

        Aplicar(resultado.Data);
        return true;
    }

    public async Task<(bool Success, string? Error)> GuardarAsync()
    {
        IsBusy = true;
        var resultado = await ApiClient.PutAsync<PerfilDto>(
            "/api/perfil",
            new { nombreCompleto = NombreCompleto, email = Email });
        IsBusy = false;

        if (!resultado.Success)
            return (false, resultado.ErrorMessage ?? "No se pudo guardar el perfil.");

        Aplicar(resultado.Data);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SubirFotoAsync(byte[] fotoBytes)
    {
        IsBusy = true;
        var base64 = Convert.ToBase64String(fotoBytes);
        var resultado = await ApiClient.PutAsync<PerfilDto>("/api/perfil/foto", new { fotoBase64 = base64 });
        IsBusy = false;

        if (!resultado.Success)
            return (false, resultado.ErrorMessage ?? "No se pudo actualizar la foto.");

        Aplicar(resultado.Data);
        return (true, null);
    }

    private void Aplicar(PerfilDto dto)
    {
        NombreCompleto = dto.NombreCompleto;
        Username = dto.Username;
        Email = dto.Email;
        FotoBase64 = dto.FotoBase64;
        Rol = dto.Rol;

        UsuarioGlobal.nurseName = dto.NombreCompleto;
    }
}
