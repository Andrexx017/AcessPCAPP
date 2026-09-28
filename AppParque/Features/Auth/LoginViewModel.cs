using System.Net;
using System.Text;
using System.Text.Json;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Auth
{
    public class LoginViewModel
    {
        public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
        {
            var result = await ApiClient.PostAsync<AuthResponseDto>("/api/auth/login", new { username, password });

            if (!result.Success)
            {
                var error = result.StatusCode == HttpStatusCode.Unauthorized
                    ? "Usuario o contraseña incorrectos."
                    : result.ErrorMessage ?? "No se pudo iniciar sesión.";
                return (false, error);
            }

            var auth = result.Data;
            await ApiClient.SetSessionAsync(auth.AccessToken, auth.RefreshToken);

            UsuarioGlobal.Role = auth.Rol;
            UsuarioGlobal.nurseName = auth.NombreCompleto;
            UsuarioGlobal.UsuarioId = ExtraerUsuarioIdDelToken(auth.AccessToken);

            return (true, null);
        }

        /// <summary>Lee el claim "sub" del JWT sin llamar a la API — el id del usuario ya viaja en el token.</summary>
        private static int ExtraerUsuarioIdDelToken(string accessToken)
        {
            try
            {
                var payload = accessToken.Split('.')[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                return int.Parse(doc.RootElement.GetProperty("sub").GetString()!);
            }
            catch
            {
                return 0;
            }
        }
    }
}
