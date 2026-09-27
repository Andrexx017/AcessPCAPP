using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

public class Condicion
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }

    public ICollection<AtraccionCondicionRestriccion> AtraccionRestricciones { get; set; } = new List<AtraccionCondicionRestriccion>();
    public ICollection<EvaluacionCondicion> EvaluacionCondiciones { get; set; } = new List<EvaluacionCondicion>();
}
