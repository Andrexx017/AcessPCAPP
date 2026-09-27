using AppParque.Api.Modules.CatalogoGruposCondiciones.Domain;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain;

public class AtraccionGrupoRestriccion
{
    public int AtraccionId { get; set; }
    public int GrupoId { get; set; }

    /// <summary>
    /// false = la atracción bloquea este grupo (si el visitante lo activó, la atracción no se preselecciona).
    /// true = la atracción está permitida exclusivamente por este grupo (ver semántica de "inclusión" del motor de reglas).
    /// </summary>
    public bool Permitido { get; set; }

    public Atraccion Atraccion { get; set; } = null!;
    public Grupo Grupo { get; set; } = null!;
}
