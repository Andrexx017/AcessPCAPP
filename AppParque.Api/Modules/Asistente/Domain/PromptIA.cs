namespace AppParque.Api.Modules.Asistente.Domain;

/// <summary>
/// Códigos de los cinco prompts editables. Separados porque cada uno gobierna una herramienta o
/// tema distinto del bot (ver AsistenteOrquestador): evaluar_atraccion, consultar_atracciones,
/// consultar_horario_parque, buscar_ayuda, y el cajón de sastre "otros". Se concatenan en ese
/// orden para formar el prompt de sistema completo, con el segmento del modo elegido en el menú
/// (ver ChatRequest.Modo) primero.
/// </summary>
public static class ClavesPrompt
{
    public const string Evaluacion = "evaluacion";
    public const string ConsultaAtracciones = "consulta_atracciones";
    public const string Horarios = "horarios";
    public const string ContextoApp = "contexto_app";
    public const string Otros = "otros";

    public static readonly string[] Todas = [Evaluacion, ConsultaAtracciones, Horarios, ContextoApp, Otros];
}

/// <summary>Un segmento editable del prompt de sistema. El admin los administra desde la pantalla
/// de Asistente IA — pestaña Prompts — sin que haga falta tocar código ni redesplegar.</summary>
public class PromptIA
{
    public int Id { get; set; }

    /// <summary>Uno de <see cref="ClavesPrompt"/>.</summary>
    public string Clave { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
