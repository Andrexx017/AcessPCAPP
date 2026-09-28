namespace AppParque.Shared.Models;

/// <summary>Datos mínimos que necesita PdfGeneratorService para dibujar una atracción en la guía —
/// desacoplado de AtraccionResultadoDto para que el generador de PDF no dependa del módulo de evaluaciones.</summary>
public class AtraccionPdfInfo
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public int? AlturaMinima { get; set; }
    public int? AlturaMaxima { get; set; }
}
