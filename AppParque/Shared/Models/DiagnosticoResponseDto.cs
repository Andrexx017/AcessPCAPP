namespace AppParque.Shared.Models;

/// <summary>
/// La respuesta de POST /api/diagnostico. Reemplaza al <see cref="EvaluacionResponseDto"/> del
/// motor de grupos en el flujo nuevo: ya no es "preseleccionada sí/no" por atracción sino un
/// veredicto de cuatro valores con los motivos que lo produjeron.
/// </summary>
public record DiagnosticoResponseDto(
    int TotalEvaluadas,
    int Permitidas,
    int ConMitigacion,
    int PorVerificar,
    int Restringidas,
    List<string> DimensionesSinDato,
    List<DictamenAtraccionDto> Atracciones);

/// <param name="Veredicto">Permitida | PermitidaConMitigacion | RequiereVerificacionPresencial | Restringida</param>
/// <param name="Motivos">Lo que decidió el veredicto. No incluye lo informativo.</param>
/// <param name="Advertencias">Lo que no decide nada pero debe salir impreso en la guía del visitante.</param>
/// <param name="Descripcion">Para la guía del visitante en PDF: lo que es la atracción.</param>
public record DictamenAtraccionDto(
    string Codigo,
    string Nombre,
    string Intensidad,
    string Rol,
    string Veredicto,
    List<MotivoDto> Motivos,
    List<string> MitigacionesRequeridas,
    List<string> Advertencias,
    string? Descripcion,
    int? AlturaMinimaCm,
    int? AlturaMaximaCm);

/// <param name="Dimension">La dimensión del perfil que lo produjo, p. ej. "pierna.protesis".
/// Permite llevar al enfermero de vuelta a la pregunta exacta.</param>
/// <param name="Severidad">Bloqueante | Verificar | Condicion | Informativo</param>
public record MotivoDto(string Codigo, string Dimension, string Texto, string Severidad);

/// <summary>Los cuatro veredictos, para no repartir strings mágicos por las vistas.</summary>
public static class Veredictos
{
    public const string Permitida = "Permitida";
    public const string ConMitigacion = "PermitidaConMitigacion";
    public const string PorVerificar = "RequiereVerificacionPresencial";
    public const string Restringida = "Restringida";
}
