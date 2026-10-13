namespace AppParque.Api.Modules.CatalogoZonas.Domain;

public class OpcionCapacidad
{
    public int Id { get; set; }
    public int PreguntaId { get; set; }
    public PreguntaCapacidad Pregunta { get; set; } = null!;

    public string Texto { get; set; } = null!;

    /// <summary>El valor que de verdad viaja al motor para las preguntas cableadas (ver
    /// PreguntaCapacidad.Clave) — para esas, es inmutable una vez creada la opción (solo se puede
    /// editar el Texto visible): es exactamente el nombre de un miembro del enum C# que
    /// PerfilBuilder.cs espera, o un valor especial que el propio PerfilBuilder reconoce por
    /// comparación literal (ej. "Sin" en prótesis de pierna). Para preguntas informativas, es texto
    /// libre.</summary>
    public string Valor { get; set; } = null!;

    public int Orden { get; set; }
}
