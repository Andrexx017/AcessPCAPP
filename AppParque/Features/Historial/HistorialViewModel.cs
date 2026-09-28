using System.Collections.ObjectModel;
using System.Text;
using AppParque.Services;
using AppParque.Shared;

namespace AppParque.Features.Historial
{
    public class HistorialViewModel : BaseViewModel
    {
        // Sentinel para la casilla "Sin restricciones": no corresponde a ningún código real del
        // catálogo de grupos/condiciones, así que no puede chocar con uno.
        private const string SinRestriccionesCodigo = "__SIN_RESTRICCIONES__";

        private List<TestRecord> _todos = new();
        private List<string> _enfermeros = new() { "Todos" };

        public ObservableCollection<TestRecord> Historial { get; } = new();
        public ObservableCollection<RestriccionFiltroItem> RestriccionOpciones { get; } = new();

        public List<string> Enfermeros
        {
            get => _enfermeros;
            private set => SetProperty(ref _enfermeros, value);
        }

        public bool MostrarFiltroEnfermero => UsuarioGlobal.EsAdmin;

        public string TotalTexto => Historial.Count == 1 ? "1 registro encontrado" : $"{Historial.Count} registros encontrados";

        public string Query { get; set; } = "";
        public bool FiltrarPorFecha { get; set; }
        public DateTime FechaDesde { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime FechaHasta { get; set; } = DateTime.Today;
        public string EnfermeroSeleccionado { get; set; } = "Todos";

        public HistorialViewModel()
        {
            _ = CargarHistorialAsync();
        }

        public async Task CargarHistorialAsync()
        {
            IsBusy = true;

            // El backend ya filtra por rol: Enfermero ve solo lo suyo, Admin ve todo (RF-08).
            var resultado = await ApiClient.GetAsync<List<HistorialItemDto>>("/api/evaluaciones");

            // El continuation de un await de red puede reanudar en un hilo de threadpool en vez del
            // hilo de UI en Windows/WinUI; mutar ObservableCollection fuera del hilo de UI hace
            // crashear el proceso a nivel nativo (no es una excepción .NET capturable). Se fuerza
            // explícitamente de vuelta al hilo de UI antes de tocar cualquier colección enlazada.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _todos.Clear();
                RestriccionOpciones.Clear();
                RestriccionOpciones.Add(new RestriccionFiltroItem(SinRestriccionesCodigo, "Sin restricciones", true, AplicarFiltros));

                if (resultado.Success)
                {
                    var catalogo = new Dictionary<string, string>();

                    foreach (var item in resultado.Data)
                    {
                        var restricciones = item.GruposActivados.Concat(item.CondicionesActivadas).ToList();
                        foreach (var r in restricciones)
                            catalogo[r.Codigo] = r.Nombre;

                        _todos.Add(new TestRecord
                        {
                            VisitorName = item.VisitanteNombre,
                            VisitorId = item.VisitanteNumeroDocumento,
                            NurseId = item.EnfermeroId,
                            NurseName = item.Enfermero,
                            FechaValue = item.Fecha.ToLocalTime(),
                            Date = item.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                            Stature = item.Estatura?.ToString() ?? "N/A",
                            Restricciones = restricciones,
                        });
                    }

                    Enfermeros = new List<string> { "Todos" }
                        .Concat(_todos.Select(t => t.NurseName).Distinct().OrderBy(n => n))
                        .ToList();

                    foreach (var (codigo, nombre) in catalogo.OrderBy(kv => kv.Value))
                        RestriccionOpciones.Add(new RestriccionFiltroItem(codigo, nombre, true, AplicarFiltros));
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo cargar el historial.", "OK");
                }

                AplicarFiltros();
            });

            IsBusy = false;
        }

        public void AplicarFiltros()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(AplicarFiltros);
                return;
            }

            IEnumerable<TestRecord> query = _todos;

            if (!string.IsNullOrWhiteSpace(Query))
            {
                var q = Query.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    (r.VisitorName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (r.VisitorId?.ToLowerInvariant().Contains(q) ?? false));
            }

            if (FiltrarPorFecha)
                query = query.Where(r => r.FechaValue.Date >= FechaDesde.Date && r.FechaValue.Date <= FechaHasta.Date);

            if (MostrarFiltroEnfermero && EnfermeroSeleccionado != "Todos")
                query = query.Where(r => r.NurseName == EnfermeroSeleccionado);

            bool incluirSinRestricciones = RestriccionOpciones.FirstOrDefault(o => o.Codigo == SinRestriccionesCodigo)?.Checked ?? true;
            var codigosDesmarcados = RestriccionOpciones
                .Where(o => o.Codigo != SinRestriccionesCodigo && !o.Checked)
                .Select(o => o.Codigo)
                .ToHashSet();

            query = query.Where(r =>
                r.Restricciones.Count == 0
                    ? incluirSinRestricciones
                    : !r.Restricciones.Any(x => codigosDesmarcados.Contains(x.Codigo)));

            Historial.Clear();
            foreach (var item in query.OrderByDescending(r => r.FechaValue))
                Historial.Add(item);

            OnPropertyChanged(nameof(TotalTexto));
        }

        public void LimpiarFiltros()
        {
            Query = "";
            FiltrarPorFecha = false;
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            EnfermeroSeleccionado = "Todos";

            foreach (var opcion in RestriccionOpciones)
                opcion.RestaurarSinDisparar(true);

            AplicarFiltros();
        }

        public async Task ExportarCsvAsync()
        {
            if (Historial.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "No hay registros para exportar con los filtros actuales.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                var path = Path.Combine(FileSystem.AppDataDirectory, $"Historial_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                var sb = new StringBuilder();
                sb.AppendLine(FilaCsv("Nombre", "Documento", "Enfermero", "Fecha", "Estatura (cm)", "Restricciones"));
                foreach (var r in Historial)
                    sb.AppendLine(FilaCsv(r.VisitorName, r.VisitorId, r.NurseName, r.Date, r.Stature, r.Answers));

                await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                bool abrir = await Application.Current.MainPage.DisplayAlert(
                    "CSV generado", $"Se exportaron {Historial.Count} registros.", "Abrir", "OK");

                if (abrir)
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo exportar el CSV: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExportarExcelAsync()
        {
            if (Historial.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "No hay registros para exportar con los filtros actuales.", "OK");
                return;
            }

            try
            {
                IsBusy = true;
                var path = Path.Combine(FileSystem.AppDataDirectory, $"Historial_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                var filas = Historial.Select(r => new
                {
                    Nombre = r.VisitorName,
                    Documento = r.VisitorId,
                    Enfermero = r.NurseName,
                    Fecha = r.Date,
                    Estatura = r.Stature,
                    Restricciones = r.Answers,
                }).ToList();

                await MiniExcelLibs.MiniExcel.SaveAsAsync(path, filas);

                bool abrir = await Application.Current.MainPage.DisplayAlert(
                    "Excel generado", $"Se exportaron {Historial.Count} registros.", "Abrir", "OK");

                if (abrir)
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo exportar el Excel: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string FilaCsv(params string[] campos) =>
            string.Join(",", campos.Select(c => $"\"{(c ?? "").Replace("\"", "\"\"")}\""));
    }

    public class RestriccionFiltroItem : BaseViewModel
    {
        private readonly Action _onChanged;
        private bool _checked;

        public string Codigo { get; }
        public string Nombre { get; }

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                OnPropertyChanged(nameof(Checked));
                _onChanged();
            }
        }

        public RestriccionFiltroItem(string codigo, string nombre, bool inicial, Action onChanged)
        {
            Codigo = codigo;
            Nombre = nombre;
            _checked = inicial;
            _onChanged = onChanged;
        }

        public void RestaurarSinDisparar(bool valor)
        {
            _checked = valor;
            OnPropertyChanged(nameof(Checked));
        }
    }
}
