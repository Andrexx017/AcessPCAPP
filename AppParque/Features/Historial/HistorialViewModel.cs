using System.Collections.ObjectModel;
using System.Text;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.Historial
{
    public class HistorialViewModel : BaseViewModel
    {
        // Claves fijas del filtro simplificado: el motor nuevo no tiene catálogo de
        // Grupos/Condiciones, así que en vez de casillas dinámicas por código se filtra por
        // veredicto y por si tuvo nota del personal o respuesta de la IA.
        private const string ConMitigacionCodigo = "con_mitigacion";
        private const string PorVerificarCodigo = "por_verificar";
        private const string RestringidaCodigo = "restringida";
        private const string ConNotaCodigo = "con_nota";

        private readonly PdfGeneratorService _pdfService = new();

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
            // Este es el historial del motor nuevo (ver HistorialEndpoints en la API) — el
            // /api/evaluaciones viejo sigue existiendo solo para "visitante recurrente, reutilizar".
            var resultado = await ApiClient.GetAsync<List<HistorialDiagnosticoItemDto>>("/api/historial");

            // El continuation de un await de red puede reanudar en un hilo de threadpool en vez del
            // hilo de UI en Windows/WinUI; mutar ObservableCollection fuera del hilo de UI hace
            // crashear el proceso a nivel nativo (no es una excepción .NET capturable). Se fuerza
            // explícitamente de vuelta al hilo de UI antes de tocar cualquier colección enlazada.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _todos.Clear();
                RestriccionOpciones.Clear();
                RestriccionOpciones.Add(new RestriccionFiltroItem(ConMitigacionCodigo, "Con condición", true, AplicarFiltros));
                RestriccionOpciones.Add(new RestriccionFiltroItem(PorVerificarCodigo, "Por verificar", true, AplicarFiltros));
                RestriccionOpciones.Add(new RestriccionFiltroItem(RestringidaCodigo, "Restringida", true, AplicarFiltros));
                RestriccionOpciones.Add(new RestriccionFiltroItem(ConNotaCodigo, "Con nota del personal o de la IA", true, AplicarFiltros));

                if (resultado.Success)
                {
                    foreach (var item in resultado.Data)
                    {
                        _todos.Add(new TestRecord
                        {
                            Id = item.Id,
                            VisitorName = item.VisitanteNombre,
                            VisitorId = item.VisitanteNumeroDocumento,
                            NurseId = item.EnfermeroId,
                            NurseName = item.Enfermero,
                            FechaValue = item.Fecha.ToLocalTime(),
                            Date = item.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                            Permitidas = item.Permitidas,
                            ConMitigacion = item.ConMitigacion,
                            PorVerificar = item.PorVerificar,
                            Restringidas = item.Restringidas,
                            TuvoCondicionAdicional = item.TuvoCondicionAdicional,
                            TuvoRespuestaIA = item.TuvoRespuestaIA,
                        });
                    }

                    Enfermeros = new List<string> { "Todos" }
                        .Concat(_todos.Select(t => t.NurseName).Distinct().OrderBy(n => n))
                        .ToList();
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

            // Cada casilla desmarcada esconde los registros que caen en esa categoría — un registro
            // sin ninguna de las cuatro (todo permitido, sin nota) nunca se esconde, porque no hay
            // casilla "sin restricciones" en este filtro simplificado.
            bool Marcada(string codigo) => RestriccionOpciones.FirstOrDefault(o => o.Codigo == codigo)?.Checked ?? true;

            var mostrarConMitigacion = Marcada(ConMitigacionCodigo);
            var mostrarPorVerificar = Marcada(PorVerificarCodigo);
            var mostrarRestringida = Marcada(RestringidaCodigo);
            var mostrarConNota = Marcada(ConNotaCodigo);

            query = query.Where(r =>
                (mostrarConMitigacion || r.ConMitigacion == 0) &&
                (mostrarPorVerificar || r.PorVerificar == 0) &&
                (mostrarRestringida || r.Restringidas == 0) &&
                (mostrarConNota || (!r.TuvoCondicionAdicional && !r.TuvoRespuestaIA)));

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
                sb.AppendLine(FilaCsv("Nombre", "Documento", "Enfermero", "Fecha", "Veredicto"));
                foreach (var r in Historial)
                    sb.AppendLine(FilaCsv(r.VisitorName, r.VisitorId, r.NurseName, r.Date, r.Answers));

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
                    Veredicto = r.Answers,
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

        /// <summary>Regenera la guía en PDF de un registro puntual del historial, sin pasar por la
        /// pantalla de preselección: incluye las atracciones que quedaron Permitida o Con condición
        /// en ese diagnóstico (mismo criterio por defecto que PreseleccionPdfViewModel). No hay
        /// descripción ni alturas porque ResultadosJson no las guarda — solo el veredicto final; el
        /// PDF las muestra como "Sin descripción"/"N/A", que PdfGeneratorService ya tolera.</summary>
        public async Task GenerarPdfAsync(TestRecord record)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var detalle = await ApiClient.GetAsync<HistorialDetalleDto>($"/api/historial/{record.Id}");
                if (!detalle.Success)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", detalle.ErrorMessage ?? "No se pudo cargar el diagnóstico.", "OK");
                    return;
                }

                var incluidas = detalle.Data.Resultados
                    .Where(r => r.Veredicto is Veredictos.Permitida or Veredictos.ConMitigacion)
                    .Select(r => new AtraccionPdfInfo
                    {
                        Nombre = r.NombreAtraccion,
                        Nota = r.Veredicto == Veredictos.ConMitigacion
                            ? (string.IsNullOrWhiteSpace(r.Comentario) ? "Permitida con condición: verificar con el personal" : r.Comentario)
                            : null,
                    })
                    .ToList();

                if (incluidas.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Este diagnóstico no tiene atracciones permitidas para incluir en el PDF.", "OK");
                    return;
                }

                var nombreArchivo = record.VisitorName.Replace(' ', '_');
                var path = Path.Combine(FileSystem.AppDataDirectory, $"Guia_{nombreArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                await _pdfService.GenerateAttractionsPdfFromListAsync(path, incluidas, record.VisitorName);

                bool abrir = await Application.Current.MainPage.DisplayAlert(
                    "PDF generado", $"Se generó la guía con {incluidas.Count} atracciones.", "Abrir", "OK");

                if (abrir)
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
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
