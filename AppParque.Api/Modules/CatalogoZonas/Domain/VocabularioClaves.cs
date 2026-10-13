namespace AppParque.Api.Modules.CatalogoZonas.Domain;

/// <summary>
/// Las claves que AppParque/Features/EvaluacionAccesibilidad/PerfilBuilder.cs (cliente MAUI) lee
/// literalmente para construir el perfil que viaja a <c>/api/diagnostico</c>. Es la única fuente de
/// verdad sobre qué preguntas están "cableadas" al motor de reglas — todo lo demás en este módulo es
/// dato administrable. Si un día se conecta una pregunta nueva al motor, hay que: (1) agregar su
/// clave acá, (2) hacer que PerfilBuilder.cs la lea, (3) enseñarle al motor (MotorCapacidades) qué
/// hacer con ella. Ninguna de esas tres cosas es un cambio de datos — por diseño.
///
/// Para una pregunta cableada, las opciones ya cargadas no se pueden agregar/quitar/editar su
/// Valor desde el admin (ver OpcionCapacidad.Valor) — el texto visible sí. Para una pregunta cuya
/// clave no aparece acá, se considera informativa: se puede editar y sus opciones son libres, pero
/// su respuesta nunca llega al motor.
/// </summary>
public static class VocabularioClaves
{
    private static readonly Dictionary<string, HashSet<string>> ClavesPorZona = new()
    {
        ["cabeza"] = ["vision", "audicion", "cognitivo", "comprende"],
        ["brazoIzq"] = ["estado", "agarre", "acortamiento", "protesisMano", "removible"],
        ["brazoDer"] = ["estado", "agarre", "acortamiento", "protesisMano", "removible"],
        ["tronco"] = ["sedestacion", "flexion", "osteomuscular", "cardiaco"],
        ["piernaIzq"] = ["estado", "protesis", "removible", "rodilla", "pie"],
        ["piernaDer"] = ["estado", "protesis", "removible", "rodilla", "pie"],
        ["marcha"] = ["capacidad", "escaleras", "embarque", "silla"],
        ["medidas"] = ["estatura", "peso", "edad"],
        ["generales"] = ["embarazo", "cirugia", "cuidado", "convulsiones", "mental", "mareo", "alturas", "claustrofobia"],
        ["contexto"] = ["rol", "presente", "adulto", "embarazoAcomp"],
    };

    /// <summary>true si "zonaCodigo.clave" es una de las que PerfilBuilder.cs ya sabe leer.</summary>
    public static bool EstaCableada(string zonaCodigo, string clave) =>
        ClavesPorZona.TryGetValue(zonaCodigo, out var claves) && claves.Contains(clave);

    /// <summary>Los 7 códigos de zona con geometría fija en el mapa del cliente — no se pueden
    /// agregar ni quitar desde el admin, solo las zonas "aparte" (EnCuerpo = false).</summary>
    public static readonly IReadOnlySet<string> ZonasEnCuerpo =
        new HashSet<string> { "cabeza", "tronco", "brazoIzq", "brazoDer", "piernaIzq", "piernaDer", "marcha" };
}
