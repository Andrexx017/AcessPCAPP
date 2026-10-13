using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Dictamen;

/// <summary>Una fila de la previsualización: lo mismo que ve el PDF, formateado para Label en vez de
/// para PdfSharpCore (por eso aquí "N/A" y el "|" sí pueden vivir en un string armado una sola vez).</summary>
public class AtraccionPreviaItem
{
    public string Nombre { get; }
    public string Descripcion { get; }
    public string Alturas { get; }
    public string? Nota { get; }
    public bool TieneNota => !string.IsNullOrWhiteSpace(Nota);

    public AtraccionPreviaItem(AtraccionPdfInfo info)
    {
        Nombre = info.Nombre;
        Descripcion = info.Descripcion ?? "Sin descripción";
        Alturas = $"Altura mínima: {info.AlturaMinima?.ToString() ?? "N/A"} cm | Altura máxima: {info.AlturaMaxima?.ToString() ?? "N/A"} cm";
        Nota = info.Nota;
    }
}

/// <summary>Genera el PDF una sola vez (al tocar Descargar o Compartir, lo que pase primero) y
/// reutiliza el archivo: las dos acciones son sobre el mismo documento, no hay razón para dibujarlo
/// dos veces.</summary>
public class VistaPreviaPdfViewModel : BaseViewModel
{
    private readonly PdfGeneratorService _pdfService = new();
    private readonly List<AtraccionPdfInfo> _seleccionadas;
    private string? _rutaGenerada;

    public List<AtraccionPreviaItem> Atracciones { get; }

    public string ResumenTexto => _seleccionadas.Count == 1
        ? "1 atracción incluida"
        : $"{_seleccionadas.Count} atracciones incluidas";

    // Mismo origen que usa PdfGeneratorService para el saludo del PDF real, para que la vista
    // previa no muestre un nombre o una fecha distintos de los que va a llevar el archivo.
    public string NombreVisitante => string.IsNullOrWhiteSpace(UsuarioGlobal.Name) ? "Visitante" : UsuarioGlobal.Name;
    public string FechaHoy => DateTime.Now.ToString("dd/MM/yyyy");

    public VistaPreviaPdfViewModel(List<AtraccionPdfInfo> seleccionadas)
    {
        _seleccionadas = seleccionadas;
        Atracciones = seleccionadas.Select(a => new AtraccionPreviaItem(a)).ToList();
    }

    public async Task DescargarAsync()
    {
        if (IsBusy) return; // evita doble-tap mientras ya se está generando

        try
        {
            IsBusy = true;
            var path = await GenerarSiNecesarioAsync();

            bool abrir = await Application.Current.MainPage.DisplayAlert(
                "✅ PDF generado",
                "El archivo se guardó correctamente",
                "Abrir", "OK");

            if (abrir)
                await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo generar el PDF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CompartirAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var path = await GenerarSiNecesarioAsync();

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Guía de atracciones",
                File = new ShareFile(path),
            });
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo compartir el PDF: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<string> GenerarSiNecesarioAsync()
    {
        if (_rutaGenerada is not null)
            return _rutaGenerada;

        var path = Path.Combine(FileSystem.AppDataDirectory, $"Guia_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        await _pdfService.GenerateAttractionsPdfFromListAsync(path, _seleccionadas);
        _rutaGenerada = path;
        return path;
    }
}
