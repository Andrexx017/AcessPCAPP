namespace AppParque.Api.Modules.CatalogoPreguntas.Domain;

public class Pregunta
{
    public int Id { get; set; }
    public string Texto { get; set; } = null!;
    public int Orden { get; set; }
    public TipoPregunta Tipo { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<OpcionRespuesta> Opciones { get; set; } = new List<OpcionRespuesta>();
}
