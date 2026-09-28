using System.Collections.ObjectModel;
using System.Windows.Input;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.ValidacionAtracciones
{
    public class ValidacionAtraccionesViewModel : BaseViewModel
    {
        private readonly PdfGeneratorService _pdfService = new();
        private readonly int _evaluacionId;

        public ObservableCollection<AttractionSelectable> Atracciones { get; } = new();

        public ICommand GenerarPDFCommand { get; }

        public ValidacionAtraccionesViewModel(EvaluacionResponseDto evaluacion)
        {
            _evaluacionId = evaluacion.Id;

            GenerarPDFCommand = new Command(async () => await GenerarPDFAsync());

            foreach (var a in evaluacion.Atracciones)
            {
                var seleccionInicial = a.ValidadaPersonal ?? a.PreseleccionadaAutomatica;
                Atracciones.Add(new AttractionSelectable(a, seleccionInicial, OnToggledAsync));
            }
        }

        private async Task OnToggledAsync(AttractionSelectable item)
        {
            var resultado = await ApiClient.PutAsync<EvaluacionResponseDto>(
                $"/api/evaluaciones/{_evaluacionId}/atracciones/{item.Model.AtraccionId}",
                new { validadaPersonal = item.IsSelected, comentario = (string?)null });

            if (!resultado.Success)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Aviso",
                    "No se pudo guardar el ajuste en el servidor. Verifica tu conexión e inténtalo de nuevo.",
                    "OK");
            }
        }

        private async Task GenerarPDFAsync()
        {
            if (IsBusy) return; // evita doble-tap mientras ya se está generando

            try
            {
                IsBusy = true;

                var seleccionadas = Atracciones.Where(x => x.IsSelected).Select(x => x.ToPdfInfo()).ToList();
                if (seleccionadas.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Selecciona al menos una atracción.", "OK");
                    return;
                }

                var path = Path.Combine(FileSystem.AppDataDirectory, $"Atracciones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                await _pdfService.GenerateAttractionsPdfFromListAsync(path, seleccionadas);

                bool abrir = await Application.Current.MainPage.DisplayAlert(
                    "✅ PDF generado",
                    "El archivo se guardó correctamente",
                    "Abrir", "OK");

                if (abrir)
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
                }
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
    }

    public class AttractionSelectable : BaseViewModel
    {
        private readonly Func<AttractionSelectable, Task> _onToggled;
        private bool _isSelected;

        public AtraccionResultadoDto Model { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                _ = _onToggled(this);
            }
        }

        public string Name => Model.AtraccionNombre;

        public string Alturas =>
            $"Altura mínima: {Model.AtraccionAlturaMinima?.ToString() ?? "N/A"} cm | Altura máxima: {Model.AtraccionAlturaMaxima?.ToString() ?? "N/A"} cm";

        public AttractionSelectable(AtraccionResultadoDto model, bool seleccionInicial, Func<AttractionSelectable, Task> onToggled)
        {
            Model = model;
            _isSelected = seleccionInicial; // set directo: no debe disparar _onToggled al construir la lista
            _onToggled = onToggled;
        }

        public AtraccionPdfInfo ToPdfInfo() => new()
        {
            Nombre = Model.AtraccionNombre,
            Descripcion = Model.AtraccionDescripcion,
            ImagenUrl = Model.AtraccionImagenUrl,
            AlturaMinima = Model.AtraccionAlturaMinima,
            AlturaMaxima = Model.AtraccionAlturaMaxima,
        };
    }
}
