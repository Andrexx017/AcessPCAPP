using AppParque.Api.Modules.CatalogoPreguntas.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionRespuesta
{
    public int EvaluacionId { get; set; }
    public int OpcionRespuestaId { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
    public OpcionRespuesta OpcionRespuesta { get; set; } = null!;
}
