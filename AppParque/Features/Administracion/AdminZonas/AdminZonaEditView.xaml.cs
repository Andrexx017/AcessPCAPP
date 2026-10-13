using System.Collections.ObjectModel;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Administracion.AdminZonas;

public partial class AdminZonaEditView : ContentPage
{
    private ZonaCapacidadAdminDto? _zonaExistente;

    public ObservableCollection<PreguntaRow> Preguntas { get; } = new();

    public AdminZonaEditView(ZonaCapacidadAdminDto? zonaExistente)
    {
        InitializeComponent();
        BindingContext = this;
        _zonaExistente = zonaExistente;

        if (_zonaExistente is null)
        {
            Title = "Nueva zona";
            panelCodigoNuevo.IsVisible = true;
            switchPermiteSinNovedad.IsToggled = true;
            entryOrden.Text = "0";
        }
        else
        {
            Title = _zonaExistente.Nombre;
            panelCodigoNuevo.IsVisible = false;
            lblCodigoExistente.IsVisible = true;
            lblCodigoExistente.Text = _zonaExistente.EnCuerpo
                ? $"Código: {_zonaExistente.Codigo} — zona \"en el cuerpo\", posición fija en el mapa."
                : $"Código: {_zonaExistente.Codigo} — zona \"aparte\".";
            panelActiva.IsVisible = true;
            separadorPreguntas.IsVisible = true;
            panelPreguntas.IsVisible = true;
            btnEliminarZona.IsVisible = !_zonaExistente.EnCuerpo;

            CargarCampos(_zonaExistente);
        }
    }

    private void CargarCampos(ZonaCapacidadAdminDto z)
    {
        entryNombre.Text = z.Nombre;
        entryCorto.Text = z.Corto;
        editorNota.Text = z.Nota;
        editorResumenSinNovedad.Text = z.ResumenSinNovedad;
        switchPermiteSinNovedad.IsToggled = z.PermiteSinNovedad;
        entryOrden.Text = z.Orden.ToString();
        switchActiva.IsToggled = z.Activa;

        Preguntas.Clear();
        foreach (var p in z.Preguntas.OrderBy(p => p.Orden))
            Preguntas.Add(new PreguntaRow(p, z.Id, OnPreguntaToggledAsync));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Vuelve aquí después de crear/editar una pregunta en la pantalla hija — recarga para que
        // la lista (y el badge "informativa") reflejen lo que haya cambiado.
        if (_zonaExistente is null)
            return;

        var resultado = await ApiClient.GetAsync<ZonaCapacidadAdminDto>($"/api/admin/zonas-capacidades/{_zonaExistente.Id}");
        if (resultado.Success)
        {
            _zonaExistente = resultado.Data;
            CargarCampos(_zonaExistente);
        }
    }

    private async Task OnPreguntaToggledAsync(PreguntaRow row)
    {
        var p = row.Model;
        var resultado = await ApiClient.PutAsync<PreguntaCapacidadAdminDto>(
            $"/api/admin/zonas-capacidades/{row.ZonaId}/preguntas/{p.Id}",
            new
            {
                clave = p.Clave,
                titulo = p.Titulo,
                ayuda = p.Ayuda,
                eje = p.Eje,
                tipo = p.Tipo,
                unidad = p.Unidad,
                orden = p.Orden,
                dependeDeClave = p.DependeDeClave,
                activaCuandoCsv = p.ActivaCuandoCsv,
                valorSinNovedad = p.ValorSinNovedad,
                activa = row.Activa,
            });

        if (!resultado.Success)
        {
            row.RevertActivaSinDisparar(!row.Activa);
            await DisplayAlert("Aviso", resultado.ErrorMessage ?? "No se pudo actualizar la pregunta.", "OK");
        }
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var nombre = entryNombre.Text?.Trim();
        var corto = entryCorto.Text?.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(corto))
        {
            await DisplayAlert("Error", "Nombre y nombre corto son obligatorios.", "OK");
            return;
        }

        if (!int.TryParse(entryOrden.Text, out var orden))
            orden = 0;

        if (_zonaExistente is null)
        {
            var codigo = entryCodigo.Text?.Trim();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                await DisplayAlert("Error", "El código es obligatorio.", "OK");
                return;
            }

            var resultado = await ApiClient.PostAsync<ZonaCapacidadAdminDto>("/api/admin/zonas-capacidades", new
            {
                codigo,
                nombre,
                corto,
                nota = editorNota.Text ?? "",
                resumenSinNovedad = editorResumenSinNovedad.Text ?? "",
                permiteSinNovedad = switchPermiteSinNovedad.IsToggled,
                orden,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo crear la zona.", "OK");
                return;
            }
        }
        else
        {
            var resultado = await ApiClient.PutAsync<ZonaCapacidadAdminDto>($"/api/admin/zonas-capacidades/{_zonaExistente.Id}", new
            {
                nombre,
                corto,
                nota = editorNota.Text ?? "",
                resumenSinNovedad = editorResumenSinNovedad.Text ?? "",
                permiteSinNovedad = switchPermiteSinNovedad.IsToggled,
                orden,
                activa = switchActiva.IsToggled,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo guardar la zona.", "OK");
                return;
            }
        }

        await Navigation.PopAsync();
    }

    private async void OnEliminarZonaClicked(object sender, EventArgs e)
    {
        if (_zonaExistente is null)
            return;

        var confirmar = await DisplayAlert("Eliminar zona", $"¿Eliminar \"{_zonaExistente.Nombre}\" y todas sus preguntas?", "Sí", "No");
        if (!confirmar)
            return;

        var resultado = await ApiClient.DeleteAsync($"/api/admin/zonas-capacidades/{_zonaExistente.Id}");
        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar la zona.", "OK");
            return;
        }

        await Navigation.PopAsync();
    }

    private async void OnNuevaPreguntaClicked(object sender, EventArgs e)
    {
        if (_zonaExistente is null)
            return;

        await Navigation.PushAsync(new AdminPreguntaCapacidadEditView(_zonaExistente.Id, preguntaExistente: null));
    }

    private async void OnPreguntaTapped(object sender, TappedEventArgs e)
    {
        if (_zonaExistente is null || e.Parameter is not PreguntaRow row)
            return;

        await Navigation.PushAsync(new AdminPreguntaCapacidadEditView(_zonaExistente.Id, row.Model));
    }
}

public class PreguntaRow : BaseViewModel
{
    private readonly Func<PreguntaRow, Task> _onToggled;
    private bool _activa;

    public PreguntaCapacidadAdminDto Model { get; }
    public int ZonaId { get; }

    public string Titulo => Model.Titulo;
    public string Clave => Model.Clave;
    public bool EsInformativa => Model.EsInformativa;

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

    public PreguntaRow(PreguntaCapacidadAdminDto model, int zonaId, Func<PreguntaRow, Task> onToggled)
    {
        Model = model;
        ZonaId = zonaId;
        _activa = model.Activa;
        _onToggled = onToggled;
    }

    public void RevertActivaSinDisparar(bool valor)
    {
        _activa = valor;
        OnPropertyChanged(nameof(Activa));
    }
}
