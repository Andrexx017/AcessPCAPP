using AppParque.Api.Modules.CatalogoAtracciones.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class EvaluacionAtraccionResultado
{
    public int Id { get; set; }
    public int EvaluacionId { get; set; }
    public int AtraccionId { get; set; }

    /// <summary>Resultado del motor de reglas, calculado automáticamente.</summary>
    public bool PreseleccionadaAutomatica { get; set; }

    /// <summary>Ajuste manual del personal sobre la preselección. Null = no revisada aún.</summary>
    public bool? ValidadaPersonal { get; set; }

    public string? Comentario { get; set; }

    public Evaluacion Evaluacion { get; set; } = null!;
    public Atraccion Atraccion { get; set; } = null!;
}
