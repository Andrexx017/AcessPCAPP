using System.Text.Json;

namespace AppParque.Api.Modules.CatalogoAtracciones.Domain.Requisitos;

/// <summary>
/// Carga y sirve las fichas de requisitos desde Data/Seed/requisitos-atracciones.json.
///
/// Vive en un archivo y no en la base de datos a propósito, por ahora: las fichas son el insumo
/// que hay que validar atracción por atracción con operaciones, y mientras eso pasa conviene que
/// un cambio sea un diff revisable en git y no un UPDATE. El paso a BD (una columna jsonb en
/// Atraccion, editable desde el módulo de administración) está en la hoja de ruta del documento
/// de estrategia, y no cambia nada del motor: solo de dónde sale este diccionario.
/// </summary>
public sealed class CatalogoRequisitos
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, RequisitosAtraccion> _porCodigo;

    private CatalogoRequisitos(IEnumerable<RequisitosAtraccion> atracciones)
    {
        _porCodigo = atracciones.ToDictionary(a => a.Codigo, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<RequisitosAtraccion> Todas => _porCodigo.Values;

    public RequisitosAtraccion? Buscar(string codigo) =>
        _porCodigo.TryGetValue(codigo, out var requisitos) ? requisitos : null;

    public static CatalogoRequisitos Cargar(string contentRootPath)
    {
        var ruta = Path.Combine(contentRootPath, "Data", "Seed", "requisitos-atracciones.json");
        var json = File.ReadAllText(ruta);

        var archivo = JsonSerializer.Deserialize<ArchivoRequisitos>(json, Opciones)
            ?? throw new InvalidOperationException($"No se pudo leer el catálogo de requisitos: {ruta}");

        if (archivo.Atracciones.Count == 0)
            throw new InvalidOperationException($"El catálogo de requisitos está vacío: {ruta}");

        var duplicados = archivo.Atracciones
            .GroupBy(a => a.Codigo, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicados.Count > 0)
            throw new InvalidOperationException(
                $"Códigos de atracción duplicados en {ruta}: {string.Join(", ", duplicados)}");

        return new CatalogoRequisitos(archivo.Atracciones);
    }

    private sealed class ArchivoRequisitos
    {
        public List<RequisitosAtraccion> Atracciones { get; set; } = [];
    }
}
