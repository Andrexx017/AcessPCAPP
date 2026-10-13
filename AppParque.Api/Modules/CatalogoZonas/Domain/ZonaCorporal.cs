namespace AppParque.Api.Modules.CatalogoZonas.Domain;

/// <summary>
/// Una zona del mapa de capacidades (ver AppParque/Features/EvaluacionAccesibilidad/CatalogoZonas.cs,
/// del que este catálogo toma el contenido inicial). Las 7 zonas con <see cref="EnCuerpo"/> = true
/// tienen una geometría de píxel fija en el mapa del cliente (MapaCapacidadesViewModel.Geometria) —
/// su <see cref="Codigo"/> NO se puede agregar ni quitar desde el admin, solo reordenar/editar/
/// activar. Las zonas "aparte" (EnCuerpo = false) no tienen esa restricción: se pueden crear,
/// eliminar y reordenar libremente.
/// </summary>
public class ZonaCorporal
{
    public int Id { get; set; }

    /// <summary>Identificador estable (ej. "cabeza", "piernaIzq"). Para una zona "en el cuerpo" debe
    /// ser uno de los 7 códigos que ya tiene geometría en el cliente — ver VocabularioClaves.</summary>
    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;
    public string Corto { get; set; } = null!;
    public string Nota { get; set; } = "";
    public string ResumenSinNovedad { get; set; } = "";
    public bool EnCuerpo { get; set; }

    /// <summary>false solo en Medidas: no hay una estatura "normal" por omisión.</summary>
    public bool PermiteSinNovedad { get; set; } = true;

    public int Orden { get; set; }
    public bool Activa { get; set; } = true;

    public ICollection<PreguntaCapacidad> Preguntas { get; set; } = new List<PreguntaCapacidad>();
}
