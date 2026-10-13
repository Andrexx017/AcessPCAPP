using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

/// <summary>Traduce un <see cref="DictamenAtraccionDto"/> a lo que necesita PdfGeneratorService.
/// Vive aparte de DictamenViewModel porque PreseleccionPdfViewModel también lo necesita: ambas
/// pantallas generan el mismo tipo de guía, solo cambia qué atracciones van en la lista.</summary>
public static class DictamenPdfMapper
{
    public static AtraccionPdfInfo ToPdfInfo(DictamenAtraccionDto atraccion)
    {
        // " · " y no "\n": XTextFormatter.DrawString de PdfSharpCore revienta con saltos de línea
        // embebidos (ver PdfGeneratorService).
        var nota = string.Join(" · ", atraccion.MitigacionesRequeridas.Select(Etiqueta).Concat(atraccion.Advertencias));

        return new AtraccionPdfInfo
        {
            Nombre = atraccion.Nombre,
            Descripcion = atraccion.Descripcion,
            AlturaMinima = atraccion.AlturaMinimaCm,
            AlturaMaxima = atraccion.AlturaMaximaCm,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota,
        };
    }

    /// <summary>Pública: DictamenViewModel.Construir también la usa para la insignia de
    /// mitigaciones en la lista, no solo para el texto del PDF.</summary>
    public static string Etiqueta(string codigoMitigacion) => codigoMitigacion switch
    {
        "acompanante_adulto" => "Debe ir con acompañante adulto",
        "misma_fila" => "El acompañante debe ir en la misma fila",
        "silla_del_centro" => "Debe ubicarse en la silla del centro",
        "silla_interna" => "Debe ubicarse en la silla interna",
        "conductor_acompanante" => "El acompañante adulto debe conducir",
        "asistencia_embarque_acompanante" => "El acompañante adulto debe asistir el embarque",
        _ => codigoMitigacion,
    };
}
