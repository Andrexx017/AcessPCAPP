namespace AppParque.Shared.Models;

/// <summary>Espejo de EstadoParqueResponse (AppParque.Api/Modules/InformacionParque/Api) — si el
/// parque está abierto ahora, calculado en el backend a partir del horario de teleféricos
/// publicado en informacion.parquedelcafe.co.</summary>
public record EstadoParqueDto(bool Abierto, string Texto, string? HoraApertura, string? HoraCierre);
