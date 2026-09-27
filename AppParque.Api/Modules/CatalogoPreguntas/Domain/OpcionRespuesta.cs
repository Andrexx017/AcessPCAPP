using AppParque.Api.Modules.Evaluaciones.Domain;

namespace AppParque.Api.Modules.CatalogoPreguntas.Domain;

public class OpcionRespuesta
{
    public int Id { get; set; }
    public int PreguntaId { get; set; }

    /// <summary>Clave estable que usa el motor de reglas (ver Modules/Evaluaciones/Application/MotorReglas.cs).
    /// El admin puede editar Texto/Orden libremente sin afectar el motor; cambiar el significado
    /// de una opción existente sí requiere ajustar el motor de reglas.</summary>
    public string Codigo { get; set; } = null!;

    public string Texto { get; set; } = null!;
    public int Orden { get; set; }

    public Pregunta Pregunta { get; set; } = null!;
    public ICollection<EvaluacionRespuesta> EvaluacionRespuestas { get; set; } = new List<EvaluacionRespuesta>();
}
