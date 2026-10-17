using AppParque.Api.Modules.Evaluaciones.Domain.Capacidades;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;

/// <summary>
/// Los requisitos de una atracción, declarados sobre las mismas dimensiones del
/// <see cref="PerfilCapacidades"/>. Se cargan desde Data/Seed/requisitos-atracciones.json, que es
/// la transcripción de las fichas de seguridad del parque (una ficha por atracción).
///
/// H-08: el sistema de seguridad depende de cada atracción y cambia, así que vive como DATO de la
/// atracción y no como rama de código. Las dos listas de abajo son ejes distintos y no se deben
/// confundir:
///   · <see cref="Dispositivos"/> restringe el APARATO (prótesis, órtesis) — es un requisito del
///     mecanismo de retención.
///   · todo lo demás exige una CAPACIDAD del cuerpo.
/// Rin-Rin es el caso que lo obliga: prohíbe la prótesis sobre rodilla pero no exige rodilla
/// completa, así que un muñón transfemoral SIN prótesis sí entra.
/// </summary>
public record RequisitosAtraccion
{
    public string Codigo { get; init; } = null!;
    public string Nombre { get; init; } = null!;

    /// <summary>baja | media | alta | extrema | cultural</summary>
    public string Intensidad { get; init; } = "media";

    public string? Descripcion { get; init; }

    /// <summary>
    /// H-02: Museo, Sendero, Show y Arte del Café son eventos culturales de uso pasivo, sin
    /// exigencia física. Con esto en true el motor devuelve "permitida" sin evaluar nada más.
    /// </summary>
    public bool SinRequisitosFisicos { get; init; }

    /// <summary>Requisitos que aplican a todo visitante de la atracción.</summary>
    public RequisitosRol General { get; init; } = new();

    /// <summary>
    /// Karts Dobles y Carros Chocones tienen requisitos distintos por rol. Lo que esté aquí se
    /// superpone sobre <see cref="General"/> para el rol que le toque.
    /// </summary>
    public Dictionary<string, RequisitosRol> PorRol { get; init; } = new();

    public RequisitosDispositivos Dispositivos { get; init; } = new();
    public RequisitosSensoriales Sensoriales { get; init; } = new();
    public RequisitosSillaRuedas SillaRuedas { get; init; } = new();
    public RequisitosAcompanante Acompanante { get; init; } = new();

    /// <summary>Códigos de <see cref="CodigosCondicion"/> que impiden el acceso.</summary>
    public List<string> RestriccionesMedicas { get; init; } = [];

    /// <summary>Condiciones que impiden el acceso al ACOMPAÑANTE, no al visitante (los dos
    /// carruseles: el acompañante adulto no debe estar embarazado).</summary>
    public List<string> RestriccionesMedicasAcompanante { get; init; } = [];

    /// <summary>
    /// Avix: "se restringe el acceso a personas que por su tamaño físico no puedan ser acomodadas
    /// de forma segura o si la barra de seguridad no cierra correctamente". Es la única cláusula
    /// redactada como juicio del operador, así que no se deduce: se marca para verificar en sitio.
    /// </summary>
    public bool ExigeAcomodacionSeguraVerificada { get; init; }

    public RequisitosRol ParaRol(RolVisitante rol)
    {
        var clave = rol switch
        {
            RolVisitante.Conductor => "conductor",
            RolVisitante.Pasajero => "pasajero",
            _ => null,
        };

        if (clave is null || !PorRol.TryGetValue(clave, out var especifico))
            return General;

        return General.Superponer(especifico);
    }

    /// <summary>true si la atracción distingue conductor de pasajero y por tanto hay que preguntar
    /// el rol antes de dar un veredicto.</summary>
    public bool DistinguePorRol => PorRol.Count > 0;
}

/// <summary>Requisitos que pueden variar por rol dentro de la misma atracción.</summary>
public record RequisitosRol
{
    public int? AlturaMinimaCm { get; init; }
    public int? AlturaMaximaCm { get; init; }

    /// <summary>H-07: solo Paseo a Caballo (100 kg).</summary>
    public int? PesoMaximoKg { get; init; }

    /// <summary>no_requiere | con_apoyo | sin_apoyo — "con_apoyo" significa que admite bastón o
    /// muletas; "sin_apoyo" exige caminar sobre los dos pies sin ningún apoyo.</summary>
    public string Marcha { get; init; } = "no_requiere";

    public bool RequiereSentarse { get; init; }
    public bool RequiereSentarseErguido { get; init; }

    /// <summary>Los carruseles piden mantenerse erguido "durante todo el recorrido", que es más
    /// exigente que poder sentarse erguido.</summary>
    public bool RequiereErguidoTodoElRecorrido { get; init; }

    public bool RequiereDoblarPiernas { get; init; }

    /// <summary>ninguno | una_mano | ambas_manos. H-09: las fichas lo declaran como umbral fijo de
    /// la atracción, no como regla cruzada con la marcha.</summary>
    public string Agarre { get; init; } = "ninguno";

    /// <summary>H-06: Paseo a Caballo, los Karts y Krater — las fichas de esas atracciones piden
    /// literalmente "sujetarse Y MANIOBRAR con ambas manos", no solo sujetarse. En botes y carros
    /// chocones el ambiente es controlado, así que basta sujetarse; en caballos, karts y Krater el
    /// visitante queda expuesto o sometido a fuerzas bruscas.</summary>
    public bool RequiereManiobrarDosManos { get; init; }

    public bool RequiereEmbarqueAutonomo { get; init; }

    /// <summary>H-05: derivado de la ficha — una atracción que no admite discapacidad cognitiva
    /// leve está exigiendo, en realidad, comprensión de instrucciones.</summary>
    public bool RequiereComprensionInstrucciones { get; init; }

    /// <summary>Devuelve esta instancia con los campos que el rol especifica sobreescritos.</summary>
    public RequisitosRol Superponer(RequisitosRol especifico) => this with
    {
        AlturaMinimaCm = especifico.AlturaMinimaCm ?? AlturaMinimaCm,
        AlturaMaximaCm = especifico.AlturaMaximaCm ?? AlturaMaximaCm,
        PesoMaximoKg = especifico.PesoMaximoKg ?? PesoMaximoKg,
        Marcha = especifico.Marcha != "no_requiere" ? especifico.Marcha : Marcha,
        RequiereSentarse = especifico.RequiereSentarse || RequiereSentarse,
        RequiereSentarseErguido = especifico.RequiereSentarseErguido || RequiereSentarseErguido,
        RequiereErguidoTodoElRecorrido = especifico.RequiereErguidoTodoElRecorrido || RequiereErguidoTodoElRecorrido,
        RequiereDoblarPiernas = especifico.RequiereDoblarPiernas || RequiereDoblarPiernas,
        Agarre = especifico.Agarre != "ninguno" ? especifico.Agarre : Agarre,
        RequiereManiobrarDosManos = especifico.RequiereManiobrarDosManos || RequiereManiobrarDosManos,
        RequiereEmbarqueAutonomo = especifico.RequiereEmbarqueAutonomo || RequiereEmbarqueAutonomo,
        RequiereComprensionInstrucciones = especifico.RequiereComprensionInstrucciones || RequiereComprensionInstrucciones,
    };
}

/// <summary>
/// El eje "aparato". Lo que restringe la ficha es el dispositivo, no el estado del cuerpo: por eso
/// hay atracciones que prohíben la prótesis y aceptan la amputación.
/// </summary>
public record RequisitosDispositivos
{
    public bool PermiteProtesisMano { get; init; } = true;
    public bool PermiteProtesisPiernaBajoRodilla { get; init; } = true;
    public bool PermiteProtesisPiernaSobreRodilla { get; init; } = true;

    /// <summary>Krater, Avix, Paseo a Caballo, Karts y Cumbre: "no permite ningún tipo de prótesis".
    /// Cuando es false gana sobre los tres flags de arriba.</summary>
    public bool PermiteCualquierProtesis { get; init; } = true;

    /// <summary>"Asegurar bien prótesis y órtesis de extremidades antes de embarcar": no bloquea,
    /// es una advertencia operativa que debe salir impresa en la guía del visitante.</summary>
    public bool ExigeAsegurarProtesis { get; init; }
}

public record RequisitosSensoriales
{
    public bool PermiteVisualParcial { get; init; } = true;
    public bool PermiteVisualTotal { get; init; } = true;
    public bool PermiteAuditiva { get; init; } = true;
    public bool PermiteCognitivaLeve { get; init; } = true;
    public bool PermiteCognitivaSeveraCertificada { get; init; }

    /// <summary>Carruseles: admiten cognitiva leve y visual total pero solo con acompañante. Es una
    /// mitigación, no un sí llano (H-10).</summary>
    public bool CognitivaLeveRequiereAcompanante { get; init; }

    public bool VisualTotalRequiereAcompanante { get; init; }
}

/// <summary>
/// Las tres formas en que una ficha modela la silla de ruedas. "NoAdmitida" es el caso por defecto
/// y significa que el visitante debe salir de la silla por sus propios medios.
/// </summary>
public record RequisitosSillaRuedas
{
    public bool Admitida { get; init; }

    /// <summary>Teleférico Bambusario, Rueda Panorámica: hay que transferirse a la cabina.</summary>
    public bool AdmiteTransferencia { get; init; }

    /// <summary>Teleférico Plaza, Tren del Café, Rueda Panorámica: hay cabina accesible y se puede
    /// permanecer en la silla, pero hay que mantenerse estable.</summary>
    public bool AdmitePermanencia { get; init; }
}

/// <summary>
/// H-10 y H-11 resueltos con las fichas: el catálogo de mitigaciones es el conjunto de
/// ADVERTENCIAS OPERATIVAS, y el umbral niño/adulto es una estatura distinta por atracción.
/// </summary>
public record RequisitosAcompanante
{
    /// <summary>Por debajo de esta estatura hace falta acompañante. null = la atracción no lo pide
    /// por estatura.</summary>
    public int? UmbralEstaturaCm { get; init; }

    /// <summary>Casi todas aceptan "acompañante adulto O persona de estatura superior a 150 cm".</summary>
    public int? EstaturaMinimaAcompananteCm { get; init; }

    public bool RequeridoPorCognitiva { get; init; }
    public bool RequeridoPorVisual { get; init; }
    public bool RequeridoPorMovilidadReducida { get; init; }

    /// <summary>Códigos de <see cref="CodigosMitigacion"/>.</summary>
    public List<string> Mitigaciones { get; init; } = [];
}

/// <summary>
/// H-10: el catálogo completo de mitigaciones que declaran las 25 fichas. No aparece el chaleco
/// (solo existía como texto suelto en la matriz vieja y ninguna ficha lo respalda).
/// </summary>
public static class CodigosMitigacion
{
    public const string AcompananteAdulto = "acompanante_adulto";
    public const string MismaFila = "misma_fila";
    public const string SillaDelCentro = "silla_del_centro";
    public const string SillaInterna = "silla_interna";
    public const string ConductorAcompanante = "conductor_acompanante";
    public const string AsistenciaEmbarqueAcompanante = "asistencia_embarque_acompanante";

    public static readonly IReadOnlyDictionary<string, string> Etiquetas = new Dictionary<string, string>
    {
        [AcompananteAdulto] = "debe ir con acompañante adulto",
        [MismaFila] = "el acompañante debe ir en la misma fila",
        [SillaDelCentro] = "debe ubicarse en la silla del centro",
        [SillaInterna] = "debe ubicarse en la silla interna",
        [ConductorAcompanante] = "el acompañante adulto debe conducir",
        [AsistenciaEmbarqueAcompanante] = "si requiere asistencia para embarque o desembarque, debe hacerlo el acompañante adulto",
    };
}
