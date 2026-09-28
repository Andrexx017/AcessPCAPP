using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminPreguntas;

public class AdminPreguntasViewModel : BaseViewModel
{
    public ObservableCollection<PreguntaRow> Preguntas { get; } = new();

    public AdminPreguntasViewModel()
    {
        _ = CargarAsync();
    }

    public async Task CargarAsync()
    {
        IsBusy = true;

        var resultado = await ApiClient.GetAsync<List<PreguntaAdminDto>>("/api/admin/preguntas");

        Preguntas.Clear();
        if (resultado.Success)
        {
            foreach (var p in resultado.Data)
                Preguntas.Add(new PreguntaRow(p, OnToggledAsync));
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo cargar el catálogo de preguntas.", "OK");
        }

        IsBusy = false;
    }

    private async Task OnToggledAsync(PreguntaRow row)
    {
        var resultado = await ApiClient.PutAsync<PreguntaAdminDto>(
            $"/api/admin/preguntas/{row.Model.Id}",
            new { texto = row.Model.Texto, tipo = row.Model.Tipo, orden = row.Model.Orden, activa = row.Activa });

        if (!resultado.Success)
        {
            row.RevertActivaSinDisparar(!row.Activa);
            await Application.Current.MainPage.DisplayAlert("Aviso", resultado.ErrorMessage ?? "No se pudo actualizar la pregunta.", "OK");
        }
    }
}

public class PreguntaRow : BaseViewModel
{
    private readonly Func<PreguntaRow, Task> _onToggled;
    private bool _activa;

    public PreguntaAdminDto Model { get; }

    public string Texto => Model.Texto;
    public string TipoEtiqueta => Model.Tipo == "SeleccionMultiple" ? "Múltiple" : "Única";

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

    public PreguntaRow(PreguntaAdminDto model, Func<PreguntaRow, Task> onToggled)
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
