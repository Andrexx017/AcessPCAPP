using AppParque.Api.Modules.Identidad.Domain;
using AppParque.Api.Modules.Visitantes.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

/// <summary>
/// Persistencia del RF-05 nuevo (motor de capacidades): hasta ahora <c>POST /api/diagnostico</c>
/// era de solo lectura (ver DiagnosticoEndpoints) y nada de lo que el personal confirmaba quedaba
/// guardado — el historial seguía colgando del motor viejo de Grupos/Condiciones, que un visitante
/// nuevo ya no alimenta. Esta entidad es lo que el botón "Confirmar y guardar en historial" crea.
///
/// Perfil y Resultados se guardan como JSON en una columna de texto (no OwnsOne/ToJson de EF):
/// es el mismo patrón que ya usa AsistenteOrquestador para los proveedores/documentos, y evita
/// mapear el árbol genérico de PerfilCapacidades (Lateralidad&lt;T&gt;) como tipo propio de EF.
/// </summary>
public class DiagnosticoGuardado
{
    public int Id { get; set; }
    public int VisitanteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string PerfilJson { get; set; } = "{}";

    /// <summary>Lo que el personal escribió a mano porque el formulario no lo cubre. No alimenta
    /// el motor de reglas — es contexto para la IA y para quien lea el historial después.</summary>
    public string? CondicionAdicional { get; set; }

    public string ResultadosJson { get; set; } = "[]";

    // Columnas escalares para que el filtro del historial no tenga que deserializar ResultadosJson.
    public int Permitidas { get; set; }
    public int ConMitigacion { get; set; }
    public int PorVerificar { get; set; }
    public int Restringidas { get; set; }

    public string? RespuestaIATexto { get; set; }
    public string? RespuestaIAModelo { get; set; }
    public Guid? RespuestaIASesionId { get; set; }

    public Visitante Visitante { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}

/// <summary>Una fila de <see cref="DiagnosticoGuardado.ResultadosJson"/>: el veredicto del motor
/// más el ajuste manual del personal, si lo hubo.</summary>
public record ResultadoAtraccionGuardado(
    string CodigoAtraccion,
    string NombreAtraccion,
    string Veredicto,
    bool? AjusteManual,
    string? Comentario);
