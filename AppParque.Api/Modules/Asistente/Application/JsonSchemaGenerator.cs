using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;

namespace AppParque.Api.Modules.Asistente.Application;

/// <summary>
/// Genera el JSON Schema del parámetro "perfil" de la herramienta evaluar_atraccion directamente
/// desde <see cref="AppParque.Api.Modules.Evaluaciones.Domain.Capacidades.PerfilCapacidades"/> por
/// reflexión, en vez de mantenerlo escrito a mano dos veces.
///
/// Por qué: el contrato de capacidades ya es "la única fuente de verdad" en todo el backend (así lo
/// dice el comentario de DiagnosticoContracts). Si lo copiara a mano acá, cada vez que alguien
/// agregue una dimensión nueva (como pasó con H-03, o como va a pasar cuando se agreguen más
/// condiciones) este archivo quedaría desincronizado sin que nada lo avise, y el modelo de IA
/// terminaría llenando un perfil que ya no es el real. Generarlo por reflexión hace que la
/// herramienta del chat SIEMPRE describa el vocabulario vigente.
///
/// La nulabilidad SÍ importa acá y no es cosmética: un modelo real (probado contra Groq) rechazó
/// su propia llamada a la herramienta porque mandó null en estaturaCm/pesoKg (int?, dato que el
/// visitante no mencionó) y el schema solo decía "integer" — y porque, al no tener forma de decir
/// "esta pierna no tiene prótesis", inventó Protesis.Nivel = "NoDeterminado", que ni siquiera es un
/// valor válido del enum (NivelProtesis no tiene NoDeterminado: "sin prótesis" es que todo el
/// objeto Protesis sea null). Por eso cada tipo nullable — int? y las referencias tipo Protesis? —
/// declara explícitamente "null" como tipo válido, leyendo la nulabilidad real de cada propiedad
/// con NullabilityInfoContext (los value types nullable ya se ven solos con Nullable.GetUnderlyingType).
/// </summary>
public static class JsonSchemaGenerator
{
    private static readonly NullabilityInfoContext NullContexto = new();

    public static JsonObject Generar(Type tipo) => GenerarInterno(tipo, [], nullable: false);

    private static JsonObject GenerarInterno(Type tipo, HashSet<Type> enProgreso, bool nullable)
    {
        var subyacente = Nullable.GetUnderlyingType(tipo);
        if (subyacente is not null)
        {
            tipo = subyacente;
            nullable = true;
        }

        var schema = ConstruirSchema(tipo, enProgreso);

        if (nullable && schema["type"] is JsonValue valorTipo && valorTipo.TryGetValue<string>(out var tipoTexto))
            schema["type"] = new JsonArray(tipoTexto, "null");

        return schema;
    }

    private static JsonObject ConstruirSchema(Type tipo, HashSet<Type> enProgreso)
    {
        if (tipo.IsEnum)
        {
            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(Enum.GetNames(tipo).Select(n => (JsonNode)n).ToArray()),
            };
        }

        if (tipo == typeof(string))
            return new JsonObject { ["type"] = "string" };

        if (tipo == typeof(int) || tipo == typeof(long))
            return new JsonObject { ["type"] = "integer" };

        if (tipo == typeof(bool))
            return new JsonObject { ["type"] = "boolean" };

        if (tipo.IsGenericType && tipo.Name.StartsWith("Lateralidad"))
        {
            var interior = GenerarInterno(tipo.GetGenericArguments()[0], enProgreso, nullable: false);
            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["izquierda"] = interior.DeepClone(),
                    ["derecha"] = interior.DeepClone(),
                },
            };
        }

        if (tipo != typeof(string) && typeof(IEnumerable).IsAssignableFrom(tipo) && tipo.IsGenericType)
        {
            var interior = GenerarInterno(tipo.GetGenericArguments()[0], enProgreso, nullable: false);
            return new JsonObject { ["type"] = "array", ["items"] = interior };
        }

        if (tipo.IsClass || (tipo.IsValueType && !tipo.IsPrimitive))
        {
            if (!enProgreso.Add(tipo))
                return new JsonObject { ["type"] = "object" }; // corta un ciclo, no debería ocurrir en este vocabulario

            var propiedades = new JsonObject();

            foreach (var prop in tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Ambas en Lateralidad<T> es derivada (un IEnumerable calculado), no un dato de entrada.
                if (prop.GetIndexParameters().Length > 0 || prop.Name == "Ambas")
                    continue;

                var nombreJson = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
                var esNullableReferencia = !prop.PropertyType.IsValueType &&
                    NullContexto.Create(prop).WriteState == NullabilityState.Nullable;

                propiedades[nombreJson] = GenerarInterno(prop.PropertyType, enProgreso, esNullableReferencia);
            }

            enProgreso.Remove(tipo);

            return new JsonObject { ["type"] = "object", ["properties"] = propiedades };
        }

        return new JsonObject { ["type"] = "string" };
    }
}
