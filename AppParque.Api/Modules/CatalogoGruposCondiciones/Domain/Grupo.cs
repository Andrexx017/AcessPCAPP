using AppParque.Api.Modules.CatalogoAtracciones.Domain;
using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

public class Grupo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }

    public ICollection<AtraccionGrupoRestriccion> AtraccionRestricciones { get; set; } = new List<AtraccionGrupoRestriccion>();
    public ICollection<EvaluacionGrupo> EvaluacionGrupos { get; set; } = new List<EvaluacionGrupo>();
}
