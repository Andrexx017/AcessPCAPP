using System.Text.RegularExpressions;
using AppParque.Shared.Models;

namespace AppParque.Features.Historial;

/// <summary>Una sección del perfil ya traducida a texto legible, para la pantalla "Ver detalles"
/// del historial.</summary>
public record DetalleSeccionTexto(string Titulo, List<string> Lineas)
{
    public bool TieneLineas => Lineas.Count > 0;
}

/// <summary>
/// Traduce un <see cref="PerfilCapacidadesDto"/> (lo que quedó guardado en DiagnosticoGuardado,
/// ver /api/historial/{id}) a texto legible. A diferencia de ResumenDiagnosticoBuilder (que arma lo
/// mismo pero a partir del recorrido en vivo por MapaCapacidadesViewModel), este trabaja solo con
/// el perfil ya resuelto — es lo único que persiste el historial, no las respuestas crudas por zona.
///
/// No traduce cada valor del vocabulario a su etiqueta exacta de pantalla (serían decenas de casos
/// repartidos en CatalogoZonas): en vez de eso separa el PascalCase del enum en palabras
/// ("SinAfectacion" → "Sin Afectacion"), suficiente para que el personal entienda la respuesta sin
/// mantener un segundo diccionario de textos.
/// </summary>
public static class PerfilCapacidadesTextoBuilder
{
    public static List<DetalleSeccionTexto> Construir(PerfilCapacidadesDto perfil)
    {
        var secciones = new List<DetalleSeccionTexto>();

        void Agregar(string titulo, params (string Etiqueta, string? Valor)[] campos)
        {
            var lineas = campos
                .Where(c => !string.IsNullOrWhiteSpace(c.Valor))
                .Select(c => $"{c.Etiqueta}: {Legible(c.Valor)}")
                .ToList();

            if (lineas.Count > 0)
                secciones.Add(new DetalleSeccionTexto(titulo, lineas));
        }

        Agregar("Pierna izquierda",
            ("Estado", perfil.Piernas.Izquierda.Estado),
            ("Rodilla completa", perfil.Piernas.Izquierda.RodillaCompleta),
            ("Pie funcional", perfil.Piernas.Izquierda.PieFuncional),
            ("Prótesis - nivel", perfil.Piernas.Izquierda.Protesis?.Nivel),
            ("Prótesis - removible", perfil.Piernas.Izquierda.Protesis?.Removible));

        Agregar("Pierna derecha",
            ("Estado", perfil.Piernas.Derecha.Estado),
            ("Rodilla completa", perfil.Piernas.Derecha.RodillaCompleta),
            ("Pie funcional", perfil.Piernas.Derecha.PieFuncional),
            ("Prótesis - nivel", perfil.Piernas.Derecha.Protesis?.Nivel),
            ("Prótesis - removible", perfil.Piernas.Derecha.Protesis?.Removible));

        Agregar("Mano izquierda",
            ("Estado", perfil.Manos.Izquierda.Estado),
            ("Agarre", perfil.Manos.Izquierda.Agarre),
            ("Acortamiento", perfil.Manos.Izquierda.Acortamiento),
            ("Prótesis - nivel", perfil.Manos.Izquierda.Protesis?.Nivel),
            ("Prótesis - removible", perfil.Manos.Izquierda.Protesis?.Removible));

        Agregar("Mano derecha",
            ("Estado", perfil.Manos.Derecha.Estado),
            ("Agarre", perfil.Manos.Derecha.Agarre),
            ("Acortamiento", perfil.Manos.Derecha.Acortamiento),
            ("Prótesis - nivel", perfil.Manos.Derecha.Protesis?.Nivel),
            ("Prótesis - removible", perfil.Manos.Derecha.Protesis?.Removible));

        Agregar("Marcha",
            ("Capacidad", perfil.Marcha.Capacidad),
            ("Apoyos", perfil.Marcha.Apoyos.Count > 0 ? string.Join(", ", perfil.Marcha.Apoyos) : null),
            ("Escaleras", perfil.Marcha.Escaleras));

        Agregar("Postura",
            ("Sedestación erguida", perfil.Postura.SedestacionErguida),
            ("Flexión de piernas", perfil.Postura.FlexionPiernas),
            ("Acomodación segura", perfil.Postura.AcomodacionSegura));

        Agregar("Embarque",
            ("Autónomo", perfil.Embarque.Autonomo),
            ("Silla de ruedas", perfil.Embarque.SillaRuedas));

        Agregar("Cuerpo",
            ("Estatura (cm)", perfil.Cuerpo.EstaturaCm?.ToString()),
            ("Peso (kg)", perfil.Cuerpo.PesoKg?.ToString()),
            ("Edad (años)", perfil.Cuerpo.EdadAnios?.ToString()));

        Agregar("Sensorial",
            ("Visión", perfil.Sensorial.Vision),
            ("Audición", perfil.Sensorial.Audicion),
            ("Cognitivo", perfil.Sensorial.Cognitivo),
            ("Comprende instrucciones", perfil.Sensorial.ComprendeInstrucciones));

        Agregar("Salud",
            ("Embarazo", perfil.Salud.Embarazo),
            ("Semanas de embarazo", perfil.Salud.SemanasEmbarazo?.ToString()),
            ("Cardiaco", perfil.Salud.Cardiaco),
            ("Osteomuscular", perfil.Salud.Osteomuscular),
            ("Cirugía reciente", perfil.Salud.CirugiaReciente),
            ("Bajo cuidado médico", perfil.Salud.BajoCuidadoMedico),
            ("Convulsiones", perfil.Salud.Convulsiones),
            ("Trastorno mental", perfil.Salud.TrastornoMental),
            ("Mareo / vértigo", perfil.Salud.MareoVertigo),
            ("Miedo a las alturas", perfil.Salud.MiedoAlturas),
            ("Claustrofobia", perfil.Salud.Claustrofobia));

        Agregar("Contexto",
            ("Rol", perfil.Contexto.Rol),
            ("Acompañante presente", perfil.Contexto.Acompanante?.Presente == true ? "Si" : null),
            ("Acompañante es adulto", perfil.Contexto.Acompanante?.Presente == true ? (perfil.Contexto.Acompanante.EsAdulto ? "Si" : "No") : null),
            ("Estatura del acompañante (cm)", perfil.Contexto.Acompanante?.EstaturaCm?.ToString()),
            ("Embarazo del acompañante", perfil.Contexto.Acompanante?.Embarazo),
            ("Acompañante puede asistir embarque", perfil.Contexto.Acompanante?.PuedeAsistirEmbarque));

        return secciones;
    }

    private static string Legible(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return "No determinado";

        return Regex.Replace(valor, "(?<!^)(?=[A-Z])", " ");
    }
}
