namespace AppParque.Features.Asistente;

/// <summary>
/// Los cinco temas del menú que se muestra antes de poder escribirle al asistente. Cada uno
/// corresponde 1 a 1 con una clave de <c>ClavesPrompt</c> en AppParque.Api
/// (Modules/Asistente/Domain/PromptIA.cs) — <see cref="Clave"/> DEBE ser idéntica a esa constante
/// (se manda tal cual en ChatRequestDto.Modo) para que el servidor sepa qué segmento de prompt
/// priorizar. Si se agrega o renombra un tema acá, hay que renombrar también del lado del API.
/// </summary>
public record ModoAsistente(string Clave, string Icono, string Titulo, string Subtitulo, string MensajeBienvenida)
{
    public static readonly ModoAsistente Evaluacion = new(
        "evaluacion", "🩺", "Diagnóstico de accesibilidad",
        "Evalúa a un visitante contra las atracciones, preguntando lo que falte.",
        "Cuéntame el caso del visitante (condiciones de salud, física o discapacidad) y te voy " +
        "preguntando lo que haga falta para darte un diagnóstico completo, atracción por atracción.");

    public static readonly ModoAsistente Restricciones = new(
        "consulta_atracciones", "📏", "Alturas y restricciones por atracción",
        "Estatura, peso, acompañante y requisitos físicos o de salud de cada atracción.",
        "Pregúntame por una atracción: estatura mínima o máxima, peso, requisitos de acompañante, " +
        "o cualquier restricción física o de salud que tenga.");

    public static readonly ModoAsistente Horarios = new(
        "horarios", "🕒", "Horarios del parque",
        "Si el parque está abierto y el horario de cada servicio.",
        "Pregúntame el horario de hoy de cualquier servicio del parque, o si está abierto ahora mismo.");

    public static readonly ModoAsistente AyudaApp = new(
        "contexto_app", "💡", "Ayuda con la aplicación",
        "Cómo usar AppParque — y te llevo directo a la pantalla si aplica.",
        "¿En qué parte de la app necesitas ayuda? Te explico cómo usarla y, si aplica, te dejo un " +
        "acceso directo a la pantalla.");

    public static readonly ModoAsistente Otros = new(
        "otros", "💬", "Otras consultas",
        "Cualquier otra pregunta relacionada con el parque.",
        "Cuéntame qué necesitas — si no es algo que pueda responder con certeza, te lo digo claro.");

    public static readonly IReadOnlyList<ModoAsistente> Todos =
        [Evaluacion, Restricciones, Horarios, AyudaApp, Otros];
}
