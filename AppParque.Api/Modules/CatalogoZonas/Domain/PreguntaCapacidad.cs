namespace AppParque.Api.Modules.CatalogoZonas.Domain;

/// <summary>
/// Una pregunta dentro de una <see cref="ZonaCorporal"/>. Si su <see cref="Clave"/> está en
/// <see cref="VocabularioClaves"/> para la zona a la que pertenece, está "cableada" al motor de
/// reglas: <c>AppParque/Features/EvaluacionAccesibilidad/PerfilBuilder.cs</c> (cliente) lee
/// literalmente "zona.clave" para construir el perfil que viaja a <c>/api/diagnostico</c>, así que
/// sus opciones no se pueden editar libremente (ver <see cref="OpcionCapacidad"/>). Si no está en el
/// vocabulario, es informativa: se pregunta y se guarda, pero nunca llega al motor.
/// </summary>
public class PreguntaCapacidad
{
    public int Id { get; set; }
    public int ZonaId { get; set; }
    public ZonaCorporal Zona { get; set; } = null!;

    /// <summary>Única dentro de su zona. Ver comentario de clase sobre el "cableado" al motor.</summary>
    public string Clave { get; set; } = null!;

    public string Titulo { get; set; } = null!;
    public string? Ayuda { get; set; }

    /// <summary>0 = ninguno · 1 estado del cuerpo · 2 dispositivo · 3 capacidad exigida.</summary>
    public int Eje { get; set; }

    public TipoRespuestaCapacidad Tipo { get; set; } = TipoRespuestaCapacidad.Opciones;

    /// <summary>Solo para Tipo = Numero (ej. "cm", "kg", "años").</summary>
    public string? Unidad { get; set; }

    public int Orden { get; set; }

    /// <summary>Clave de otra pregunta de la misma zona de la que depende esta — null si no depende
    /// de ninguna.</summary>
    public string? DependeDeClave { get; set; }

    /// <summary>Valores de <see cref="DependeDeClave"/> que activan esta pregunta, separados por coma
    /// (ej. "Si", o "BajoRodilla,SobreRodilla"). Solo tiene sentido si DependeDeClave no es null.</summary>
    public string? ActivaCuandoCsv { get; set; }

    /// <summary>El valor que se guarda para esta pregunta cuando la zona se marca "sin novedad".
    /// Null si la zona no tiene un valor normal por omisión para esta pregunta.</summary>
    public string? ValorSinNovedad { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<OpcionCapacidad> Opciones { get; set; } = new List<OpcionCapacidad>();
}
