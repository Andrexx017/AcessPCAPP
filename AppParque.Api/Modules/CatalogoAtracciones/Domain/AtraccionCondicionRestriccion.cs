using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain;

public class AtraccionCondicionRestriccion
{
    public int AtraccionId { get; set; }
    public int CondicionId { get; set; }

    /// <summary>
    /// false = la atracción bloquea esta condición (si el visitante la tiene, la atracción no se preselecciona).
    /// true = la atracción está permitida exclusivamente por esta condición.
    /// </summary>
    public bool Permitido { get; set; }

    public Atraccion Atraccion { get; set; } = null!;
    public Condicion Condicion { get; set; } = null!;
}
