using AppParque.Api.Modules.Identidad.Domain;

namespace AppParque.Api.Modules.Identidad.Application;

public interface ITokenService
{
    string CreateAccessToken(Usuario usuario);

    (string RawToken, RefreshToken Entity) CreateRefreshToken(int usuarioId);

    string HashToken(string rawToken);
}
