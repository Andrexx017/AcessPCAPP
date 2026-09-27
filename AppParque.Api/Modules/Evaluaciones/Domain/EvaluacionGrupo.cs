using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionGrupo
{
    public int EvaluacionId { get; set; }
    public int GrupoId { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
    public Grupo Grupo { get; set; } = null!;
}
