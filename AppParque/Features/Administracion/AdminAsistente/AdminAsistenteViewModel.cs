using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Administracion.AdminAsistente;

public class AdminAsistenteViewModel : BaseViewModel
{
    public ObservableCollection<ProveedorRow> Proveedores { get; } = new();
    public ObservableCollection<DocumentoRow> Documentos { get; } = new();
    public ObservableCollection<PromptRow> Prompts { get; } = new();

    public AdminAsistenteViewModel()
    {
        _ = CargarAsync();
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var proveedores = await ApiClient.GetAsync<List<ProveedorIADto>>("/api/admin/asistente/proveedores");
        Proveedores.Clear();
        if (proveedores.Success)
            foreach (var p in proveedores.Data.OrderBy(p => p.Prioridad))
                Proveedores.Add(new ProveedorRow(p));

        var documentos = await ApiClient.GetAsync<List<DocumentoContextoDto>>("/api/admin/asistente/documentos");
        Documentos.Clear();
        if (documentos.Success)
            foreach (var d in documentos.Data)
                Documentos.Add(new DocumentoRow(d));

        var prompts = await ApiClient.GetAsync<List<PromptIADto>>("/api/admin/asistente/prompts");
        Prompts.Clear();
        if (prompts.Success)
            foreach (var p in prompts.Data)
                Prompts.Add(new PromptRow(p));

        IsBusy = false;
    }
}

public class PromptRow : BaseViewModel
{
    public PromptIADto Model { get; }

    public string Clave => Model.Clave;
    public string Titulo => Model.Titulo;
    public string Subtitulo => Clave switch
    {
        "evaluacion" => "Gobierna evaluar_atraccion — cómo decide acceso para un visitante.",
        "consulta_atracciones" => "Gobierna consultar_atracciones — preguntas generales del catálogo.",
        "horarios" => "Gobierna consultar_horario_parque — horarios del parque.",
        "contexto_app" => "Gobierna buscar_ayuda — cómo usar la app.",
        "otros" => "Cajón de sastre — lo que no cae en los otros temas.",
        _ => "",
    };

    private string _contenido;
    public string Contenido
    {
        get => _contenido;
        set { _contenido = value; OnPropertyChanged(); }
    }

    public PromptRow(PromptIADto model)
    {
        Model = model;
        _contenido = model.Contenido;
    }
}

public class ProveedorRow
{
    public ProveedorIADto Model { get; }

    public string Nombre => Model.Nombre;
    public string Modelo => Model.Modelo;
    public string PrioridadTexto => $"Prioridad {Model.Prioridad}";

    public string EstadoTexto => Model.UltimoEstado switch
    {
        "ok" => "● Activo ahora",
        "limite_alcanzado" => "⚠ Límite alcanzado",
        "error" => "✕ Error",
        _ => "— Sin probar",
    };

    public Color EstadoColor => Model.UltimoEstado switch
    {
        "ok" => Color.FromArgb("#0A4D42"),
        "limite_alcanzado" => Color.FromArgb("#6B4600"),
        "error" => Color.FromArgb("#8A2317"),
        _ => Color.FromArgb("#8b93a1"),
    };

    public bool EsActivo => Model.Activo;
    public double Opacidad => Model.Activo ? 1.0 : 0.55;

    public ProveedorRow(ProveedorIADto model)
    {
        Model = model;
    }
}

public class DocumentoRow
{
    public DocumentoContextoDto Model { get; }

    public string Nombre => Model.Nombre;
    public string Detalle => $"{Model.LongitudContenido:N0} caracteres · cargado {Model.CargadoEn.ToLocalTime():dd/MM/yyyy}";
    public double Opacidad => Model.Activo ? 1.0 : 0.55;

    public DocumentoRow(DocumentoContextoDto model)
    {
        Model = model;
    }
}
