using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminZonas;

public class AdminZonasViewModel : BaseViewModel
{
    public ObservableCollection<ZonaRow> ZonasCuerpo { get; } = new();
    public ObservableCollection<ZonaRow> ZonasAparte { get; } = new();

    public async Task CargarAsync()
    {
        IsBusy = true;

        var resultado = await ApiClient.GetAsync<List<ZonaCapacidadAdminDto>>("/api/admin/zonas-capacidades");

        ZonasCuerpo.Clear();
        ZonasAparte.Clear();

        if (resultado.Success)
        {
            foreach (var z in resultado.Data.OrderBy(z => z.Orden))
            {
                var row = new ZonaRow(z, OnToggledAsync);
                (z.EnCuerpo ? ZonasCuerpo : ZonasAparte).Add(row);
            }
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo cargar el catálogo del test.", "OK");
        }

        IsBusy = false;
    }

    private async Task OnToggledAsync(ZonaRow row)
    {
        var z = row.Model;
        var resultado = await ApiClient.PutAsync<ZonaCapacidadAdminDto>(
            $"/api/admin/zonas-capacidades/{z.Id}",
            new { nombre = z.Nombre, corto = z.Corto, nota = z.Nota, resumenSinNovedad = z.ResumenSinNovedad, permiteSinNovedad = z.PermiteSinNovedad, orden = z.Orden, activa = row.Activa });

        if (!resultado.Success)
        {
            row.RevertActivaSinDisparar(!row.Activa);
            await Application.Current!.MainPage!.DisplayAlert("Aviso", resultado.ErrorMessage ?? "No se pudo actualizar la zona.", "OK");
        }
    }
}

public class ZonaRow : BaseViewModel
{
    private readonly Func<ZonaRow, Task> _onToggled;
    private bool _activa;

    public ZonaCapacidadAdminDto Model { get; }

    public string Nombre => Model.Nombre;
    public string Detalle => $"{Model.Preguntas.Count} pregunta(s) · {Model.Preguntas.Count(p => p.EsInformativa)} informativa(s)";

    public bool Activa
    {
        get => _activa;
        set
        {
            if (_activa == value) return;
            _activa = value;
            OnPropertyChanged(nameof(Activa));
            _ = _onToggled(this);
        }
    }

    public ZonaRow(ZonaCapacidadAdminDto model, Func<ZonaRow, Task> onToggled)
    {
        Model = model;
        _activa = model.Activa;
        _onToggled = onToggled;
    }

    public void RevertActivaSinDisparar(bool valor)
    {
        _activa = valor;
        OnPropertyChanged(nameof(Activa));
    }
}
