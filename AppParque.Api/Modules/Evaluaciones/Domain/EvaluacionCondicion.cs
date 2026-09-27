using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionCondicion
{
    public int EvaluacionId { get; set; }
    public int CondicionId { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
    public Condicion Condicion { get; set; } = null!;
}
