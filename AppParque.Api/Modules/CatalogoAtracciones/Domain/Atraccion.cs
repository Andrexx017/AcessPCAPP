using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain;

public class Atraccion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public int? AlturaMinima { get; set; }
    public int? AlturaMaxima { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<AtraccionGrupoRestriccion> RestriccionesGrupo { get; set; } = new List<AtraccionGrupoRestriccion>();
    public ICollection<AtraccionCondicionRestriccion> RestriccionesCondicion { get; set; } = new List<AtraccionCondicionRestriccion>();
    public ICollection<EvaluacionAtraccionResultado> EvaluacionResultados { get; set; } = new List<EvaluacionAtraccionResultado>();
}
