using AppParque.Api.Modules.Identidad.Domain;
using AppParque.Api.Modules.Visitantes.Domain;

namespace AppParque.Api.Modules.Evaluaciones.Domain;

public class Evaluacion
{
    public int Id { get; set; }
    public int VisitanteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? Edad { get; set; }
    public int? Estatura { get; set; }

    public Visitante Visitante { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;

    public ICollection<EvaluacionRespuesta> Respuestas { get; set; } = new List<EvaluacionRespuesta>();
    public ICollection<EvaluacionGrupo> Grupos { get; set; } = new List<EvaluacionGrupo>();
    public ICollection<EvaluacionCondicion> Condiciones { get; set; } = new List<EvaluacionCondicion>();
    public ICollection<EvaluacionAtraccionResultado> AtraccionResultados { get; set; } = new List<EvaluacionAtraccionResultado>();
}
