using System.Text;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.EvaluacionAccesibilidad;

/// <summary>
/// Pantalla de Resumen (Opción A del mockup) entre el mapa de zonas y el dictamen: lista lo
/// contestado, deja agregar una condición de texto libre que el formulario no cubre, y permite
/// preguntarle a la IA antes de seguir. No toca <see cref="PerfilCapacidades"/> — la condición
/// adicional es contexto para la IA y para el historial, nunca entra al motor de reglas.
/// </summary>
public class ResumenDiagnosticoViewModel : BaseViewModel
{
    private readonly MapaCapacidadesViewModel _vm;
    private string _condicionAdicional = string.Empty;

    public ResumenDiagnosticoViewModel(MapaCapacidadesViewModel vm)
    {
        _vm = vm;
        Secciones = ResumenDiagnosticoBuilder.Construir(vm);
    }

    public IReadOnlyList<ResumenZonaItem> Secciones { get; }

    public string NombreVisitante => string.IsNullOrWhiteSpace(UsuarioGlobal.Name)
        ? "Visitante sin nombre"
        : UsuarioGlobal.Name;

    public string DatosVisitante => string.Join(" · ", new[]
    {
        string.IsNullOrWhiteSpace(UsuarioGlobal.Age) ? null : $"{UsuarioGlobal.Age} años",
        string.IsNullOrWhiteSpace(UsuarioGlobal.Stature) ? null : $"{UsuarioGlobal.Stature} cm",
    }.Where(s => s is not null));

    public int TotalMarcadas => Secciones.Sum(s => s.Preguntas.Count(p => p.EsAlerta));

    public string CondicionAdicional
    {
        get => _condicionAdicional;
        set => SetProperty(ref _condicionAdicional, value);
    }

    public string? RespuestaIA { get; private set; }
    public string? RespuestaIAModelo { get; private set; }
    public Guid? RespuestaIASesionId { get; private set; }
    public bool TieneRespuestaIA => !string.IsNullOrWhiteSpace(RespuestaIA);

    /// <summary>La tarjeta de la IA se muestra siempre (nunca IsVisible=false→true dinámico): en
    /// algunas combinaciones de MAUI, un elemento que arranca colapsado dentro de un ScrollView no
    /// se vuelve a medir cuando IsVisible pasa a true más tarde, y queda invisible aunque el dato ya
    /// esté puesto. Mostrar un texto de marcador de posición hasta tener respuesta evita ese riesgo.</summary>
    public string RespuestaIATexto => TieneRespuestaIA
        ? RespuestaIA!
        : "Toca \"Preguntar IA\" para ver aquí su lectura del diagnóstico.";

    public string? UltimoError { get; private set; }

    /// <summary>El perfil con el que terminó evaluando <see cref="PedirDictamenAsync"/> — la
    /// pantalla de confirmación lo necesita para guardar exactamente lo que se evaluó.</summary>
    public PerfilCapacidadesDto? UltimoPerfil => _vm.UltimoPerfil;

    public async Task<bool> PreguntarIaAsync()
    {
        // Igual que HistorialViewModel.CargarHistorialAsync: el continuation de un await de red
        // puede reanudar fuera del hilo de UI, y los cambios de propiedad enlazados a la vista no
        // se reflejan (sin excepción, sin crash — simplemente no repintan) si no se fuerza de
        // vuelta al hilo de UI antes de tocarlos.
        await MainThread.InvokeOnMainThreadAsync(() => IsBusy = true);

        try
        {
            var mensaje = ConstruirMensajeIa();
            var resultado = await ApiClient.PostAsync<ChatResponseDto>(
                "/api/asistente/chat",
                new ChatRequestDto(mensaje, null, null));

            return await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!resultado.Success)
                {
                    UltimoError = resultado.ErrorMessage ?? "No se pudo consultar a la IA.";
                    return false;
                }

                RespuestaIA = resultado.Data.Respuesta;
                RespuestaIAModelo = resultado.Data.ModeloUsado;
                RespuestaIASesionId = resultado.Data.SesionId;
                OnPropertyChanged(nameof(RespuestaIA));
                OnPropertyChanged(nameof(RespuestaIATexto));
                OnPropertyChanged(nameof(TieneRespuestaIA));
                return true;
            });
        }
        catch (ApiException ex)
        {
            UltimoError = ex.Message;
            return false;
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
        }
    }

    public Task<DiagnosticoResponseDto?> PedirDictamenAsync() => _vm.PedirDictamenAsync();

    public string? UltimoErrorDictamen => _vm.UltimoError;

    /// <summary>Perfil resumido en líneas clave:valor + la nota del personal en prosa — el modelo
    /// ya tiene una herramienta (evaluar_atraccion) que sabe leer un PerfilCapacidades, así que no
    /// hace falta un contrato nuevo, solo describírselo con suficiente detalle.</summary>
    private string ConstruirMensajeIa()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Evalúa el diagnóstico de accesibilidad de {NombreVisitante}" +
            (string.IsNullOrWhiteSpace(DatosVisitante) ? "." : $" ({DatosVisitante})."));
        sb.AppendLine("Respuestas del test guiado:");
        sb.AppendLine(ResumenDiagnosticoBuilder.ComoTexto(Secciones));

        if (!string.IsNullOrWhiteSpace(CondicionAdicional))
        {
            sb.AppendLine();
            sb.AppendLine($"Observación adicional del personal, no cubierta por el formulario: \"{CondicionAdicional.Trim()}\"");
        }

        sb.AppendLine();
        sb.AppendLine("Explica qué atracciones quedarían restringidas o con condición, y por qué, teniendo en cuenta también la observación adicional aunque el motor de reglas no la evalúe directamente.");

        return sb.ToString();
    }
}
