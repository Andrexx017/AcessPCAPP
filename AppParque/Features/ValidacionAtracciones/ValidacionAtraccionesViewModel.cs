using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AppParque.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.IO;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.ValidacionAtracciones
{
    public class ValidacionAtraccionesViewModel : BaseViewModel
    {
        private readonly FireBaseService _firebaseService;
        private readonly PdfGeneratorService _pdfService;
        private readonly Restrictions _restricciones;
        private readonly int _estatura;

        public ObservableCollection<AttractionSelectable> Atracciones { get; } = new();

        public ICommand GenerarPDFCommand { get; }

        public ValidacionAtraccionesViewModel(Restrictions restricciones, int estatura)
        {
            _restricciones = restricciones;
            _estatura = estatura;

            _firebaseService = new FireBaseService();
            _pdfService = new PdfGeneratorService();

            GenerarPDFCommand = new Command(async () => await GenerarPDFAsync());

            _ = LoadAtraccionesAsync();
        }

        private async Task LoadAtraccionesAsync()
        {
            try
            {
                var dict = await _firebaseService.GetAttractionsAsync();
                if (dict == null || dict.Count == 0) return;

                foreach (var a in dict.Values)
                {
                    // ✅ Filtro por estatura
                    if (a.stature_min.HasValue && _estatura < a.stature_min.Value) continue;
                    if (a.stature_max.HasValue && _estatura > a.stature_max.Value) continue;

                    // Por defecto la atracción se marca seleccionada
                    var selectable = new AttractionSelectable(a) { IsSelected = true };

                    if (a.restrictions != null)
                    {
                        bool cumpleCondiciones = true;

                        // 🟢 CASO ESPECIAL: todas las condiciones/grupos en true => atracción libre
                        bool todasCondicionesTrue = a.restrictions.condiciones != null &&
                                                    a.restrictions.condiciones.All(c => c.Value == true);
                        bool todosGruposTrue = a.restrictions.grupos != null &&
                                               a.restrictions.grupos.All(g => g.Value == true);

                        if (!(todasCondicionesTrue && todosGruposTrue))
                        {
                            // 🚫 1. Validar bloqueos (false en JSON)
                            if (a.restrictions.condiciones != null)
                            {
                                foreach (var kv in a.restrictions.condiciones)
                                {
                                    if (kv.Value == false &&   // atracción no permite esta condición
                                        _restricciones.condiciones.ContainsKey(kv.Key) &&
                                        _restricciones.condiciones[kv.Key]) // usuario tiene esa condición
                                    {
                                        cumpleCondiciones = false;
                                        break;
                                    }
                                }
                            }

                            // ✅ 2. Validar inclusiones (true en JSON)
                            if (cumpleCondiciones && a.restrictions.condiciones != null)
                            {
                                var inclusiones = a.restrictions.condiciones.Where(c => c.Value == true).ToList();

                                if (inclusiones.Any())
                                {
                                    bool esExclusiva = inclusiones.Count == a.restrictions.condiciones.Count;

                                    if (esExclusiva)
                                    {
                                        bool usuarioCumpleAlguna = inclusiones.Any(c =>
                                            _restricciones.condiciones.ContainsKey(c.Key) &&
                                            _restricciones.condiciones[c.Key]);

                                        if (!usuarioCumpleAlguna)
                                            cumpleCondiciones = false;
                                    }
                                }
                            }

                            // 🚦 3. Validar grupos
                            if (cumpleCondiciones && a.restrictions.grupos != null)
                            {
                                foreach (var kv in a.restrictions.grupos)
                                {
                                    if (kv.Value == false &&
                                        _restricciones.grupos.ContainsKey(kv.Key) &&
                                        _restricciones.grupos[kv.Key])
                                    {
                                        cumpleCondiciones = false;
                                        break;
                                    }
                                }
                            }
                        }

                        selectable.IsSelected = cumpleCondiciones;
                    }

                    Atracciones.Add(selectable);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudieron cargar atracciones: {ex.Message}", "OK");
            }
        }



        private async Task GenerarPDFAsync()
        {
            try
            {
                var seleccionadas = Atracciones.Where(x => x.IsSelected).Select(x => x.Model).ToList();
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
        }
    }

    public class AttractionSelectable : BaseViewModel
    {
        private bool _isSelected;

        public Attraction Model { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // ✅ Nueva propiedad para enlazar en XAML
        public string Name => Model.name;

        public string Alturas =>
            $"Altura mínima: {Model.stature_min?.ToString() ?? "N/A"} cm | Altura máxima: {Model.stature_max?.ToString() ?? "N/A"} cm";

        public AttractionSelectable(Attraction model)
        {
            Model = model;
        }
    }
}
