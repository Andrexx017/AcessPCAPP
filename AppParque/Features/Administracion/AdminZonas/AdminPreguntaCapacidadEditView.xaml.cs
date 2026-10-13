using System.Collections.ObjectModel;
using AppParque.Services;

namespace AppParque.Features.Administracion.AdminZonas;

public partial class AdminPreguntaCapacidadEditView : ContentPage
{
    private readonly int _zonaId;
    private readonly PreguntaCapacidadAdminDto? _preguntaExistente;

    /// <summary>Si la pregunta está cableada al motor, no se pueden agregar/quitar opciones ni
    /// cambiar su Valor — ver VocabularioClaves del lado del servidor. Para una pregunta nueva se
    /// asume libre: si el servidor termina marcándola cableada (clave coincide con una ya
    /// conectada), rechaza el guardado con un mensaje claro.</summary>
    private readonly bool _puedeEditarValores;

    public ObservableCollection<OpcionEditRow> Opciones { get; } = new();

    public AdminPreguntaCapacidadEditView(int zonaId, PreguntaCapacidadAdminDto? preguntaExistente)
    {
        InitializeComponent();
        BindingContext = this;
        _zonaId = zonaId;
        _preguntaExistente = preguntaExistente;
        _puedeEditarValores = preguntaExistente is null || preguntaExistente.EsInformativa;

        if (_preguntaExistente is null)
        {
            Title = "Nueva pregunta";
            entryClave.IsEnabled = true;
            lblEstado.Text = "Si la clave no coincide con ninguna ya conectada al motor de reglas, esta pregunta queda como informativa: se pregunta y se guarda, pero no decide ningún dictamen.";
            frameEstado.BackgroundColor = Color.FromArgb("#FDF8EC");
            lblEstado.TextColor = Color.FromArgb("#6B4600");
            switchActiva.IsToggled = true;
            entryOrden.Text = "0";
            pickerEje.SelectedIndex = 0;
            pickerTipo.SelectedIndex = 0;
        }
        else
        {
            Title = _preguntaExistente.Titulo;
            entryClave.IsEnabled = false;
            btnEliminarPregunta.IsVisible = true;

            if (_preguntaExistente.EsInformativa)
            {
                lblEstado.Text = "Informativa: se pregunta y se guarda en el resumen/historial, pero no está conectada al motor de reglas — no decide ningún dictamen.";
                frameEstado.BackgroundColor = Color.FromArgb("#FDF8EC");
                lblEstado.TextColor = Color.FromArgb("#6B4600");
            }
            else
            {
                lblEstado.Text = "Cableada al motor de reglas: sus opciones (el \"Valor\" de cada una) no se pueden agregar, quitar ni cambiar — solo el texto visible. El resto de la pregunta sí es editable.";
                frameEstado.BackgroundColor = Color.FromArgb("#FBEAE6");
                lblEstado.TextColor = Color.FromArgb("#8A2317");
            }

            CargarCampos(_preguntaExistente);
        }

        btnAgregarOpcion.IsVisible = _puedeEditarValores;
        ActualizarVisibilidadPorTipo();
    }

    private void CargarCampos(PreguntaCapacidadAdminDto p)
    {
        entryClave.Text = p.Clave;
        editorTitulo.Text = p.Titulo;
        editorAyuda.Text = p.Ayuda;
        pickerEje.SelectedIndex = Math.Clamp(p.Eje, 0, 3);
        pickerTipo.SelectedIndex = p.Tipo == "Numero" ? 1 : 0;
        entryUnidad.Text = p.Unidad;
        entryOrden.Text = p.Orden.ToString();
        entryDependeDe.Text = p.DependeDeClave;
        entryActivaCuando.Text = p.ActivaCuandoCsv;
        entryValorSinNovedad.Text = p.ValorSinNovedad;
        switchActiva.IsToggled = p.Activa;

        Opciones.Clear();
        foreach (var o in p.Opciones.OrderBy(o => o.Orden))
            Opciones.Add(new OpcionEditRow { Id = o.Id, Texto = o.Texto, Valor = o.Valor, Orden = o.Orden, PuedeEliminar = _puedeEditarValores });
    }

    private void OnTipoCambiado(object sender, EventArgs e) => ActualizarVisibilidadPorTipo();

    private void ActualizarVisibilidadPorTipo()
    {
        var esNumero = pickerTipo.SelectedIndex == 1;
        panelUnidad.IsVisible = esNumero;
        panelOpciones.IsVisible = !esNumero;
        collectionOpciones.IsVisible = !esNumero;
        separadorOpciones.IsVisible = !esNumero;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        var titulo = editorTitulo.Text?.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            await DisplayAlert("Error", "El título es obligatorio.", "OK");
            return;
        }

        var tipo = pickerTipo.SelectedIndex == 1 ? "Numero" : "Opciones";
        var eje = pickerEje.SelectedIndex < 0 ? 0 : pickerEje.SelectedIndex;
        if (!int.TryParse(entryOrden.Text, out var orden))
            orden = 0;

        var ayuda = string.IsNullOrWhiteSpace(editorAyuda.Text) ? null : editorAyuda.Text;
        var unidad = string.IsNullOrWhiteSpace(entryUnidad.Text) ? null : entryUnidad.Text;
        var dependeDe = string.IsNullOrWhiteSpace(entryDependeDe.Text) ? null : entryDependeDe.Text.Trim();
        var activaCuando = string.IsNullOrWhiteSpace(entryActivaCuando.Text) ? null : entryActivaCuando.Text.Trim();
        var valorSinNovedad = string.IsNullOrWhiteSpace(entryValorSinNovedad.Text) ? null : entryValorSinNovedad.Text.Trim();

        if (_preguntaExistente is null)
        {
            var clave = entryClave.Text?.Trim();
            if (string.IsNullOrWhiteSpace(clave))
            {
                await DisplayAlert("Error", "La clave es obligatoria.", "OK");
                return;
            }

            var opcionesRequest = tipo == "Opciones"
                ? Opciones.Select(o => new { texto = o.Texto, valor = o.Valor, orden = o.Orden }).ToList<object>()
                : new List<object>();

            var resultado = await ApiClient.PostAsync<PreguntaCapacidadAdminDto>($"/api/admin/zonas-capacidades/{_zonaId}/preguntas", new
            {
                clave,
                titulo,
                ayuda,
                eje,
                tipo,
                unidad,
                orden,
                dependeDeClave = dependeDe,
                activaCuandoCsv = activaCuando,
                valorSinNovedad,
                activa = switchActiva.IsToggled,
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
            var resultado = await ApiClient.PutAsync<PreguntaCapacidadAdminDto>(
                $"/api/admin/zonas-capacidades/{_zonaId}/preguntas/{_preguntaExistente.Id}", new
                {
                    clave = _preguntaExistente.Clave,
                    titulo,
                    ayuda,
                    eje,
                    tipo,
                    unidad,
                    orden,
                    dependeDeClave = dependeDe,
                    activaCuandoCsv = activaCuando,
                    valorSinNovedad,
                    activa = switchActiva.IsToggled,
                });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo guardar la pregunta.", "OK");
                return;
            }
        }

        await Navigation.PopAsync();
    }

    private async void OnEliminarPreguntaClicked(object sender, EventArgs e)
    {
        if (_preguntaExistente is null)
            return;

        var confirmar = await DisplayAlert("Eliminar pregunta", $"¿Eliminar \"{_preguntaExistente.Titulo}\"?", "Sí", "No");
        if (!confirmar)
            return;

        var resultado = await ApiClient.DeleteAsync($"/api/admin/zonas-capacidades/{_zonaId}/preguntas/{_preguntaExistente.Id}");
        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar la pregunta.", "OK");
            return;
        }

        await Navigation.PopAsync();
    }

    // --- opciones ------------------------------------------------------------------------------

    private async void OnAgregarOpcionClicked(object sender, EventArgs e)
    {
        var texto = await DisplayPromptAsync("Nueva opción", "Texto visible:");
        if (string.IsNullOrWhiteSpace(texto))
            return;

        var valor = await DisplayPromptAsync("Nueva opción", "Valor (código interno, sin espacios):");
        if (string.IsNullOrWhiteSpace(valor))
            return;

        if (_preguntaExistente is null)
        {
            // Pregunta aún no creada: se acumula localmente y se envía junto con el POST.
            Opciones.Add(new OpcionEditRow { Id = 0, Texto = texto, Valor = valor, Orden = Opciones.Count, PuedeEliminar = true });
            return;
        }

        var resultado = await ApiClient.PostAsync<OpcionCapacidadAdminDto>(
            $"/api/admin/zonas-capacidades/{_zonaId}/preguntas/{_preguntaExistente.Id}/opciones",
            new { texto, valor, orden = Opciones.Count });

        if (!resultado.Success)
        {
            await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo agregar la opción.", "OK");
            return;
        }

        Opciones.Add(new OpcionEditRow { Id = resultado.Data.Id, Texto = resultado.Data.Texto, Valor = resultado.Data.Valor, Orden = resultado.Data.Orden, PuedeEliminar = true });
    }

    private async void OnEditarOpcionClicked(object sender, EventArgs e)
    {
        if (((Button)sender).CommandParameter is not OpcionEditRow row)
            return;

        var nuevoTexto = await DisplayPromptAsync("Editar opción", "Texto visible:", initialValue: row.Texto);
        if (string.IsNullOrWhiteSpace(nuevoTexto))
            return;

        var nuevoValor = row.Valor;
        if (_puedeEditarValores)
        {
            var valorIngresado = await DisplayPromptAsync("Editar opción", "Valor (código interno):", initialValue: row.Valor);
            if (string.IsNullOrWhiteSpace(valorIngresado))
                return;
            nuevoValor = valorIngresado;
        }

        if (_preguntaExistente is not null && row.Id != 0)
        {
            var resultado = await ApiClient.PutAsync<OpcionCapacidadAdminDto>(
                $"/api/admin/zonas-capacidades/{_zonaId}/preguntas/{_preguntaExistente.Id}/opciones/{row.Id}",
                new { texto = nuevoTexto, valor = nuevoValor, orden = row.Orden });

            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo editar la opción.", "OK");
                return;
            }
        }

        row.Texto = nuevoTexto;
        row.Valor = nuevoValor;
        var indice = Opciones.IndexOf(row);
        Opciones.RemoveAt(indice);
        Opciones.Insert(indice, row);
    }

    private async void OnEliminarOpcionClicked(object sender, EventArgs e)
    {
        if (((Button)sender).CommandParameter is not OpcionEditRow row)
            return;

        var confirmar = await DisplayAlert("Eliminar opción", $"¿Eliminar \"{row.Texto}\"?", "Sí", "No");
        if (!confirmar)
            return;

        if (_preguntaExistente is not null && row.Id != 0)
        {
            var resultado = await ApiClient.DeleteAsync($"/api/admin/zonas-capacidades/{_zonaId}/preguntas/{_preguntaExistente.Id}/opciones/{row.Id}");
            if (!resultado.Success)
            {
                await DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo eliminar.", "OK");
                return;
            }
        }

        Opciones.Remove(row);
    }
}

public class OpcionEditRow
{
    public int Id { get; set; }
    public string Texto { get; set; } = "";
    public string Valor { get; set; } = "";
    public int Orden { get; set; }
    public bool PuedeEliminar { get; set; } = true;
}
