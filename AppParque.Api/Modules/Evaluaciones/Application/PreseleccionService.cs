using AppParque.Api.Modules.CatalogoAtracciones.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Application;

/// <summary>
/// Cruza los grupos/condiciones activados por un visitante (más su estatura) contra las
/// restricciones de una atracción. Requiere que RestriccionesGrupo/RestriccionesCondicion
/// vengan cargadas junto con su Grupo/Condicion (Include + ThenInclude).
///
/// Semántica verificada contra los datos reales de Data/Seed/admin.json (no la descripción original
/// de "inclusión exclusiva" de la sección 5 del documento de requisitos, que no coincide con cómo
/// está cargada la matriz real): Permitido=false en una fila bloquea la atracción si el visitante
/// activó ese código; Permitido=true es simplemente "no bloquea". Si un código activado no tiene
/// fila de restricción para esa atracción, no bloquea (comportamiento por defecto permisivo).
/// </summary>
public static class PreseleccionService
{
    public static bool EsSeguraPara(
        Atraccion atraccion,
        IReadOnlySet<string> gruposActivados,
        IReadOnlySet<string> condicionesActivadas,
        int? estatura)
    {
        if (estatura.HasValue)
        {
            if (atraccion.AlturaMinima.HasValue && estatura.Value < atraccion.AlturaMinima.Value)
                return false;

            if (atraccion.AlturaMaxima.HasValue && estatura.Value > atraccion.AlturaMaxima.Value)
                return false;
        }

        foreach (var restriccion in atraccion.RestriccionesGrupo)
        {
            if (!restriccion.Permitido && gruposActivados.Contains(restriccion.Grupo.Codigo))
                return false;
        }

        foreach (var restriccion in atraccion.RestriccionesCondicion)
        {
            if (!restriccion.Permitido && condicionesActivadas.Contains(restriccion.Condicion.Codigo))
                return false;
        }

        return true;
    }
}
