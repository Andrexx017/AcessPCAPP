using System.Collections.ObjectModel;
using AppParque.Features.Administracion.AdminAtracciones;
using AppParque.Features.Historial;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Administracion.Inicio;

/// <summary>
/// Datos reales que la API expone: catálogo de atracciones (intensidad, estatura mínima, activa),
/// el listado de evaluaciones (fecha, visitante, restricciones activadas), el agregado de aptitud
/// del día (/api/evaluaciones/aptitud-hoy) y el conteo de consultas al Asistente IA del día
/// (/api/admin/asistente/consultas-hoy).
/// </summary>
public class InicioAdminViewModel : BaseViewModel
{
    public ObservableCollection<IntensidadRow> PorIntensidad { get; } = new();
    public ObservableCollection<RestriccionAlturaRow> Restricciones { get; } = new();
    public ObservableCollection<UltimaEvaluacionRow> UltimasEvaluaciones { get; } = new();

    private int _evaluacionesHoy;
    public int EvaluacionesHoy
    {
        get => _evaluacionesHoy;
        set => SetProperty(ref _evaluacionesHoy, value);
    }

    private int _visitantesHoy;
    public int VisitantesHoy
    {
        get => _visitantesHoy;
        set => SetProperty(ref _visitantesHoy, value);
    }

    private int _totalAtracciones;
    public int TotalAtracciones
    {
        get => _totalAtracciones;
        set => SetProperty(ref _totalAtracciones, value);
    }

    private int _atraccionesActivas;
    public int AtraccionesActivas
    {
        get => _atraccionesActivas;
        set => SetProperty(ref _atraccionesActivas, value);
    }

    private string _porcentajeAptosTexto = "—";
    public string PorcentajeAptosTexto
    {
        get => _porcentajeAptosTexto;
        set => SetProperty(ref _porcentajeAptosTexto, value);
    }

    private string _aptitudSubtexto = "Cargando...";
    public string AptitudSubtexto
    {
        get => _aptitudSubtexto;
        set => SetProperty(ref _aptitudSubtexto, value);
    }

    private int _consultasIAHoy;
    public int ConsultasIAHoy
    {
        get => _consultasIAHoy;
        set => SetProperty(ref _consultasIAHoy, value);
    }

    private string _parqueTituloTexto = "Consultando horario...";
    public string ParqueTituloTexto
    {
        get => _parqueTituloTexto;
        set => SetProperty(ref _parqueTituloTexto, value);
    }

    private string _parqueEstadoTexto = "";
    public string ParqueEstadoTexto
    {
        get => _parqueEstadoTexto;
        set => SetProperty(ref _parqueEstadoTexto, value);
    }

    private Color _parqueColor = Color.FromArgb("#9A8C76");
    public Color ParqueColor
    {
        get => _parqueColor;
        set => SetProperty(ref _parqueColor, value);
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var atraccionesTask = ApiClient.GetAsync<List<AtraccionAdminDto>>("/api/admin/atracciones");
        var evaluacionesTask = ApiClient.GetAsync<List<HistorialItemDto>>("/api/evaluaciones");
        var aptitudTask = ApiClient.GetAsync<AptitudHoyDto>("/api/evaluaciones/aptitud-hoy");
        var consultasTask = ApiClient.GetAsync<ConsultasHoyDto>("/api/admin/asistente/consultas-hoy");
        var horarioTask = ApiClient.GetAsync<EstadoParqueDto>("/api/parque/horario");
        await Task.WhenAll(atraccionesTask, evaluacionesTask, aptitudTask, consultasTask, horarioTask);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            AplicarAtracciones(atraccionesTask.Result);
            AplicarEvaluaciones(evaluacionesTask.Result);
            AplicarAptitud(aptitudTask.Result);
            AplicarConsultasIA(consultasTask.Result);
            AplicarHorarioParque(horarioTask.Result);
        });

        IsBusy = false;
    }

    private void AplicarHorarioParque(ApiResult<EstadoParqueDto> resultado)
    {
        if (!resultado.Success)
        {
            ParqueTituloTexto = "Horario no disponible";
            ParqueEstadoTexto = "No se pudo consultar el horario";
            ParqueColor = Color.FromArgb("#9A8C76");
            return;
        }

        ParqueTituloTexto = resultado.Data.Abierto ? "Parque abierto" : "Parque cerrado";
        ParqueEstadoTexto = resultado.Data.Texto;
        ParqueColor = Color.FromArgb(resultado.Data.Abierto ? "#3E7D44" : "#B23A2A");
    }

    private void AplicarAptitud(ApiResult<AptitudHoyDto> resultado)
    {
        if (!resultado.Success)
        {
            PorcentajeAptosTexto = "—";
            AptitudSubtexto = "No disponible";
            return;
        }

        var data = resultado.Data;
        if (data.Total == 0)
        {
            PorcentajeAptosTexto = "—";
            AptitudSubtexto = "Sin evaluaciones hoy";
            return;
        }

        var porcentaje = (int)Math.Round(data.Aptos * 100.0 / data.Total);
        PorcentajeAptosTexto = $"{porcentaje}%";
        AptitudSubtexto = $"{data.Aptos} de {data.Total} resultados";
    }

    private void AplicarConsultasIA(ApiResult<ConsultasHoyDto> resultado)
    {
        ConsultasIAHoy = resultado.Success ? resultado.Data.Total : 0;
    }

    private void AplicarAtracciones(ApiResult<List<AtraccionAdminDto>> resultado)
    {
        if (!resultado.Success)
            return;

        var atracciones = resultado.Data;
        TotalAtracciones = atracciones.Count;
        AtraccionesActivas = atracciones.Count(a => a.Activa);

        PorIntensidad.Clear();
        foreach (var clave in new[] { "baja", "media", "alta", "extrema", "cultural" })
        {
            var items = atracciones.Where(a => a.Intensidad == clave).ToList();
            if (items.Count == 0) continue;
            PorIntensidad.Add(new IntensidadRow(clave, items.Count, items.Count(a => a.Activa)));
        }

        Restricciones.Clear();
        foreach (var grupo in atracciones
                     .Where(a => a.AlturaMinima.HasValue)
                     .GroupBy(a => a.AlturaMinima!.Value)
                     .OrderByDescending(g => g.Count())
                     .ThenByDescending(g => g.Key))
        {
            Restricciones.Add(new RestriccionAlturaRow(grupo.Key, grupo.Count()));
        }
    }

    private void AplicarEvaluaciones(ApiResult<List<HistorialItemDto>> resultado)
    {
        if (!resultado.Success)
            return;

        var evaluaciones = resultado.Data;
        var hoy = DateTime.Today;
        var deHoy = evaluaciones.Where(e => e.Fecha.ToLocalTime().Date == hoy).ToList();

        EvaluacionesHoy = deHoy.Count;
        VisitantesHoy = deHoy.Select(e => e.VisitanteId).Distinct().Count();

        UltimasEvaluaciones.Clear();
        foreach (var item in evaluaciones.OrderByDescending(e => e.Fecha).Take(5))
        {
            var totalRestricciones = item.GruposActivados.Count + item.CondicionesActivadas.Count;
            UltimasEvaluaciones.Add(new UltimaEvaluacionRow(
                item.VisitanteNombre,
                item.Fecha.ToLocalTime().ToString("hh:mm tt"),
                totalRestricciones));
        }
    }
}

/// <param name="Clave">baja | media | alta | extrema | cultural.</param>
public record IntensidadRow(string Clave, int Total, int Activas)
{
    public string Nombre => Clave switch
    {
        "baja" => "Baja",
        "media" => "Media",
        "alta" => "Alta",
        "extrema" => "Extrema",
        "cultural" => "Cultural",
        _ => Clave,
    };

    public Color Punto => Clave switch
    {
        "baja" => Color.FromArgb("#3E7D44"),
        "media" => Color.FromArgb("#D9A400"),
        "alta" => Color.FromArgb("#E07B2A"),
        "extrema" => Color.FromArgb("#B23A2A"),
        _ => Color.FromArgb("#9C3F97"), // cultural
    };

    public string Resumen => $"{Total} · {Activas} activa{(Activas == 1 ? "" : "s")}";
}

public record RestriccionAlturaRow(int AlturaMinima, int Cantidad)
{
    public string Texto => $"Estatura mínima {AlturaMinima} cm";
    public string CantidadTexto => Cantidad == 1 ? "1 atracción" : $"{Cantidad} atracciones";
}

public record UltimaEvaluacionRow(string Visitante, string Hora, int TotalRestricciones)
{
    public string RestriccionesTexto => TotalRestricciones == 0 ? "Sin restricciones" : $"{TotalRestricciones} restricción(es)";
    public Color BadgeFondo => TotalRestricciones == 0 ? Color.FromArgb("#E9F5E1") : Color.FromArgb("#FBEAE6");
    public Color BadgeTexto => TotalRestricciones == 0 ? Color.FromArgb("#3E7D44") : Color.FromArgb("#B23A2A");
}

/// <summary>Espejo de AptitudHoyResponse (AppParque.Api/Modules/Evaluaciones/Api).</summary>
public record AptitudHoyDto(int Total, int Aptos, int NoAptos);

/// <summary>Espejo de ConsultasHoyResponse (AppParque.Api/Modules/Asistente/Api).</summary>
public record ConsultasHoyDto(int Total);
