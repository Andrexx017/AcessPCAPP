using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppParque.Services;

/// <summary>
/// Reemplaza a FireBaseService: toda la comunicación de la app con AppParque.Api pasa por aquí.
/// Sigue el mismo estilo "estático + estado en memoria" que ya usa UsuarioGlobal en este proyecto,
/// en vez de inyección de dependencias (el resto de la app tampoco usa DI para las páginas).
/// </summary>
public static class ApiClient
{
    // 10.0.2.2 es la IP con la que el emulador de Android ve al "localhost" de la máquina host
    // que corre docker-compose. En Windows/otros, localhost sí resuelve directo.
#if ANDROID
    public const string BaseUrl = "http://10.0.2.2:8080";
#else
    public const string BaseUrl = "http://localhost:8080";
#endif

    private const string AccessTokenKey = "appparque_access_token";
    private const string RefreshTokenKey = "appparque_refresh_token";

    // 120s y no 30s: /api/asistente/chat puede recorrer una cadena de proveedores (uno rate-limited
    // cae rápido, pero el siguiente puede ser un modelo de razonamiento que necesita varias rondas
    // de tool calling a 4-6s cada una) y legítimamente tardar más de 30s sin que nada esté roto.
    // Un servidor caído de verdad falla casi al instante (connection refused), así que esto no
    // demora detectar un problema real — solo deja de matar respuestas lentas pero válidas.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,

        // Las propiedades nulas NO se envían. Es obligatorio para /api/diagnostico: el perfil de
        // capacidades manda los enums como texto y los del backend no son nullable, así que un
        // "campo": null hace fallar la deserialización completa. Omitirlo deja el valor por
        // defecto del enum (NoDeterminado), que es justo lo que significa "no se preguntó".
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // GET /api/zonas-capacidades manda PreguntaZona.Tipo como texto ("Opciones"/"Numero") —
        // sin este converter, System.Text.Json solo acepta enums como número.
        Converters = { new JsonStringEnumConverter() },
    };

    public static string? AccessToken { get; private set; }
    public static string? RefreshToken { get; private set; }

    public static bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public static async Task LoadSessionAsync()
    {
        AccessToken = await SecureStorage.GetAsync(AccessTokenKey);
        RefreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
    }

    public static async Task SetSessionAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
    }

    public static void ClearSession()
    {
        AccessToken = null;
        RefreshToken = null;
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
    }

    public static async Task<ApiResult<T>> GetAsync<T>(string path)
    {
        var response = await SendAsync(HttpMethod.Get, path);
        return await ReadResultAsync<T>(response);
    }

    public static async Task<ApiResult<TResponse>> PostAsync<TResponse>(string path, object body)
    {
        var response = await SendAsync(HttpMethod.Post, path, body);
        return await ReadResultAsync<TResponse>(response);
    }

    public static async Task<ApiResult<object?>> PostAsync(string path, object body)
    {
        var response = await SendAsync(HttpMethod.Post, path, body);
        return await ReadResultAsync<object?>(response);
    }

    public static async Task<ApiResult<TResponse>> PutAsync<TResponse>(string path, object body)
    {
        var response = await SendAsync(HttpMethod.Put, path, body);
        return await ReadResultAsync<TResponse>(response);
    }

    public static async Task<ApiResult<object?>> DeleteAsync(string path)
    {
        var response = await SendAsync(HttpMethod.Delete, path);
        return await ReadResultAsync<object?>(response);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, bool permitirReintento = true)
    {
        var request = new HttpRequestMessage(method, $"{BaseUrl}{path}");
        if (!string.IsNullOrEmpty(AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            // SendAsync no recibe un CancellationToken propio acá, así que un TaskCanceledException
            // solo puede venir de HttpClient.Timeout — nunca de una cancelación pedida por el
            // usuario. Separarlo del catch general evita el mensaje engañoso "no se pudo conectar"
            // cuando en realidad el servidor seguía procesando una respuesta lenta (ver el timeout
            // de 120s más arriba).
            throw new ApiException("El servidor tardó demasiado en responder. Intenta de nuevo.");
        }
        catch (Exception ex)
        {
            throw new ApiException($"No se pudo conectar con el servidor ({ex.Message}).");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized && permitirReintento && !string.IsNullOrEmpty(RefreshToken))
        {
            if (await TryRefreshAsync())
                return await SendAsync(method, path, body, permitirReintento: false);
        }

        return response;
    }

    private static async Task<bool> TryRefreshAsync()
    {
        try
        {
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/api/auth/refresh", new { refreshToken = RefreshToken });
            if (!response.IsSuccessStatusCode)
            {
                ClearSession();
                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<RefreshAuthResponse>(JsonOptions);
            if (auth is null)
            {
                ClearSession();
                return false;
            }

            await SetSessionAsync(auth.AccessToken, auth.RefreshToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return ApiResult<T>.Ok(default!);

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return ApiResult<T>.Ok(data!);
        }

        string? mensaje = null;
        try
        {
            mensaje = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            // ignorar: nos quedamos con el mensaje genérico de abajo
        }

        return ApiResult<T>.Error(response.StatusCode, string.IsNullOrWhiteSpace(mensaje) ? response.ReasonPhrase : mensaje);
    }

    private sealed record RefreshAuthResponse(string AccessToken, string RefreshToken);
}

public class ApiException(string message) : Exception(message);

public class ApiResult<T>
{
    public bool Success { get; private init; }
    public T Data { get; private init; } = default!;
    public HttpStatusCode? StatusCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResult<T> Error(HttpStatusCode statusCode, string? message) =>
        new() { Success = false, StatusCode = statusCode, ErrorMessage = message };
}
