namespace AppParque.Api.Modules.Asistente.Domain;

/// <summary>
/// Un documento cargado por el admin para que la herramienta <c>buscar_ayuda</c> del chat lo
/// consulte — guías de cómo usar cada pantalla, políticas internas, lo que haga falta para que el
/// personal nuevo se adapte más rápido. Texto plano por ahora (sin parseo de PDF en el servidor):
/// el cliente MAUI extrae el texto antes de subirlo.
/// </summary>
public class DocumentoContexto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Contenido { get; set; } = null!;

    public DateTime CargadoEn { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}
