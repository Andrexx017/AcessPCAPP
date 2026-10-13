namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>Una respuesta ya traducida a texto legible, para la pantalla de Resumen.</summary>
public record ResumenPreguntaItem(string Titulo, string RespuestaTexto, bool EsAlerta);

/// <summary>Una zona del mapa con las preguntas que de verdad se contestaron. Zonas sin ninguna
/// respuesta no generan sección — no hay nada que resumir.</summary>
public record ResumenZonaItem(string ZonaId, string Nombre, IReadOnlyList<ResumenPreguntaItem> Preguntas)
{
    public int Respondidas => Preguntas.Count;
    public bool TieneAlertas => Preguntas.Any(p => p.EsAlerta);

    /// <summary>Versión de una sola línea, para la vista condensada del diagnóstico en
    /// DictamenView — ahí ya no hace falta la fila pregunta/respuesta completa.</summary>
    public string ResumenTexto => string.Join(" · ", Preguntas.Select(p => $"{p.Titulo} {p.RespuestaTexto}"));
}

/// <summary>
/// Traduce las respuestas crudas del recorrido por zonas a algo que un humano pueda leer, para la
/// pantalla de Resumen que se muestra antes de pedir el dictamen (Opción A del mockup). Usa
/// <see cref="MapaCapacidadesViewModel.PreguntasActivas"/> en vez de recorrer
/// <see cref="CatalogoZonas"/> a mano para no duplicar la lógica de dependencias entre preguntas.
///
/// "Alerta" aquí es solo una marca visual (la respuesta difiere del valor de «sin novedad» de su
/// zona) — no implica que la atracción quede restringida; eso todavía no se sabe hasta que el
/// motor evalúa el perfil completo.
/// </summary>
public static class ResumenDiagnosticoBuilder
{
    public static IReadOnlyList<ResumenZonaItem> Construir(MapaCapacidadesViewModel vm)
    {
        var secciones = new List<ResumenZonaItem>();

        foreach (var zona in CatalogoZonas.Todas)
        {
            var items = new List<ResumenPreguntaItem>();

            foreach (var pregunta in vm.PreguntasActivas(zona))
            {
                var valor = vm.Respuesta($"{zona.Id}.{pregunta.Clave}");
                if (string.IsNullOrWhiteSpace(valor))
                    continue;

                var texto = pregunta.Tipo == TipoRespuestaZona.Numero
                    ? $"{valor} {pregunta.Unidad}".Trim()
                    : pregunta.OpcionesSeguras.FirstOrDefault(o => o.Valor == valor)?.Texto ?? valor;

                var esAlerta = zona.ValoresSinNovedad.TryGetValue(pregunta.Clave, out var valorDefecto) && valorDefecto != valor;

                items.Add(new ResumenPreguntaItem(pregunta.Titulo, texto, esAlerta));
            }

            if (items.Count > 0)
                secciones.Add(new ResumenZonaItem(zona.Id, zona.Nombre, items));
        }

        return secciones;
    }

    /// <summary>El mismo resumen, en líneas "zona · pregunta: respuesta", para meter dentro del
    /// mensaje que se le manda a la IA (ver ResumenDiagnosticoViewModel.PreguntarIaAsync).</summary>
    public static string ComoTexto(IReadOnlyList<ResumenZonaItem> secciones) =>
        string.Join("\n", secciones.SelectMany(z =>
            z.Preguntas.Select(p => $"- {z.Nombre} · {p.Titulo} {p.RespuestaTexto}")));
}
