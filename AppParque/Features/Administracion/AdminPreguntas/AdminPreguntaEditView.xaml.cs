using System.Collections.ObjectModel;
using AppParque.Services;

namespace AppParque.Features.Administracion.AdminPreguntas;

public partial class AdminPreguntaEditView : ContentPage
{
    private readonly PreguntaAdminDto? _preguntaExistente;

    public ObservableCollection<OpcionEditRow> Opciones { get; } = new();

    public AdminPreguntaEditView(PreguntaAdminDto? preguntaExistente)
    {
        InitializeComponent();
        BindingContext = this;
        _preguntaExistente = preguntaExistente;

        if (_preguntaExistente is null)
        {
            Title = "Nueva pregunta";
        }
        else
        {
            Title = "Editar pregunta";
            editorTexto.Text = _preguntaExistente.Texto;
            pickerTipo.SelectedItem = _preguntaExistente.Tipo == "SeleccionMultiple" ? "Selección múltiple" : "Única selección";
            entryOrden.Text = _preguntaExistente.Orden.ToString();
            switchActiva.IsToggled = _preguntaExistente.Activa;

            foreach (var o in _preguntaExistente.Opciones)
                Opciones.Add(new OpcionEditRow { Id = o.Id, Codigo = o.Codigo, Texto = o.Texto, Orden = o.Orden });
        }
    }

    private static string TipoATextoBackend(string tipoLegible) =>
        tipoLegible == "Selección múltiple" ? "SeleccionMultiple" : "UnicaSeleccion";

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var texto = editorTexto.Text?.Trim();
        var tipoLegible = pickerTipo.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(tipoLegible))
        {
            await DisplayAlert("Error", "Escriba el texto y seleccione el tipo de pregunta.", "OK");
            return;
        }

        if (!int.TryParse(entryOrden.Text, out var orden))
            orden = 0;

        var tipo = TipoATextoBackend(tipoLegible);
        var activa = switchActiva.IsToggled;

        if (_preguntaExistente is null)
        {
            var opcionesRequest = Opciones.Select((o, i) => new { codigo = o.Codigo, texto = o.Texto, orden = i }).ToList();

            var resultado = await ApiClient.PostAsync<PreguntaAdminDto>("/api/admin/preguntas", new
            {
                texto,
                tipo,
                orden,
                activa,
                opciones = opcionesRequest,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo crear la pregunta.", "OK");
                return;
            }
        }
        else
        {
            var resultado = await ApiClient.PutAsync<PreguntaAdminDto>($"/api/admin/preguntas/{_preguntaExistente.Id}", new
            {
                texto,
                tipo,
                orden,
                activa,
            });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo actualizar la pregunta.", "OK");
                return;
            }
        }

        await Navigation.PopAsync();
    }

    private async void OnAgregarOpcionClicked(object sender, EventArgs e)
    {
        var codigo = await DisplayPromptAsync("Nueva opción", "Código (único, ej. si_camina):");
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        var textoOpcion = await DisplayPromptAsync("Nueva opción", "Texto visible:");
        if (string.IsNullOrWhiteSpace(textoOpcion))
            return;

        if (_preguntaExistente is null)
        {
            // Pregunta aún no creada: se acumula localmente y se envía junto con el POST de la pregunta.
            Opciones.Add(new OpcionEditRow { Id = 0, Codigo = codigo, Texto = textoOpcion, Orden = Opciones.Count });
            return;
        }

        var resultado = await ApiClient.PostAsync<OpcionRespuestaAdminDto>(
            $"/api/admin/preguntas/{_preguntaExistente.Id}/opciones",
            new { codigo, texto = textoOpcion, orden = Opciones.Count });

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo agregar la opción.", "OK");
            return;
        }

        Opciones.Add(new OpcionEditRow { Id = resultado.Data.Id, Codigo = resultado.Data.Codigo, Texto = resultado.Data.Texto, Orden = resultado.Data.Orden });
    }

    private async void OnEditarOpcionClicked(object sender, EventArgs e)
    {
        if (((Button)sender).CommandParameter is not OpcionEditRow row)
            return;

        var nuevoTexto = await DisplayPromptAsync("Editar opción", "Texto visible:", initialValue: row.Texto);
        if (string.IsNullOrWhiteSpace(nuevoTexto))
            return;

        if (_preguntaExistente is not null && row.Id != 0)
        {
            var resultado = await ApiClient.PutAsync<OpcionRespuestaAdminDto>(
                $"/api/admin/preguntas/{_preguntaExistente.Id}/opciones/{row.Id}",
                new { codigo = row.Codigo, texto = nuevoTexto, orden = row.Orden });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo editar la opción.", "OK");
                return;
            }
        }

        row.Texto = nuevoTexto;
        var indice = Opciones.IndexOf(row);
        Opciones.RemoveAt(indice);
        Opciones.Insert(indice, row);
    }

    private async void OnEliminarOpcionClicked(object sender, EventArgs e)
    {
        if (((Button)sender).CommandParameter is not OpcionEditRow row)
            return;

        bool confirmar = await DisplayAlert("Eliminar opción", $"¿Eliminar \"{row.Texto}\"?", "Sí", "No");
        if (!confirmar)
            return;

        if (_preguntaExistente is not null && row.Id != 0)
        {
            var resultado = await ApiClient.DeleteAsync($"/api/admin/preguntas/{_preguntaExistente.Id}/opciones/{row.Id}");
            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar la opción.", "OK");
                return;
            }
        }

        Opciones.Remove(row);
    }
}

public class OpcionEditRow
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Texto { get; set; } = "";
    public int Orden { get; set; }
}
