using System.Collections.ObjectModel;
using System.Windows.Input;
using AppParque.Features.ValidacionAtracciones;
using AppParque.Services;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Features.EvaluacionAccesibilidad
{
    public class TestViewModel : BaseViewModel
    {
        private int currentQuestionIndex = 0;
        private List<Question> questions = new();

        public ObservableCollection<OptionDisplay> CurrentOptions { get; set; } = new();

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }

        public TestViewModel()
        {
            NextCommand = new Command(async () => await NextQuestionAsync());
            PreviousCommand = new Command(PreviousQuestion);

            _ = CargarPreguntasAsync();
        }

        private async Task CargarPreguntasAsync()
        {
            IsBusy = true;

            var resultado = await ApiClient.GetAsync<List<PreguntaDto>>("/api/preguntas");

            if (resultado.Success && resultado.Data.Count > 0)
            {
                questions = resultado.Data
                    .OrderBy(p => p.Orden)
                    .Select(p => new Question
                    {
                        Id = p.Id,
                        Text = p.Texto,
                        IsMultipleChoice = p.Tipo == "SeleccionMultiple",
                        Options = p.Opciones.OrderBy(o => o.Orden).Select(o => new OptionDisplay(o)).ToList(),
                    })
                    .ToList();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    resultado.ErrorMessage ?? "No se pudieron cargar las preguntas del test.",
                    "OK");
            }

            IsBusy = false;
            UpdateBindings();
        }

        public string CurrentQuestionText => questions.Count == 0
            ? (IsBusy ? "Cargando preguntas..." : "No hay preguntas disponibles")
            : questions[currentQuestionIndex].Text;

        public string ProgressText => questions.Count == 0 ? string.Empty : $"Pregunta {currentQuestionIndex + 1} de {questions.Count}";
        public double Progress => questions.Count == 0 ? 0 : (double)(currentQuestionIndex + 1) / questions.Count;
        public bool IsLastQuestion => questions.Count > 0 && currentQuestionIndex == questions.Count - 1;
        public bool CanGoBack => currentQuestionIndex > 0;
        public bool CanGoNext => !IsBusy && questions.Count > 0;

        public Question GetCurrentQuestion()
        {
            if (questions.Count > 0 && currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
                return questions[currentQuestionIndex];
            return null;
        }

        private async Task NextQuestionAsync()
        {
            if (questions.Count == 0)
                return;

            if (currentQuestionIndex < questions.Count - 1)
            {
                currentQuestionIndex++;
                UpdateBindings();
                return;
            }

            await FinalizarTestAsync();
        }

        private async Task FinalizarTestAsync()
        {
            var opcionesRespuestaIds = questions.SelectMany(q => q.SelectedOptionIds).Distinct().ToList();

            if (opcionesRespuestaIds.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Aviso", "Responde al menos una pregunta antes de continuar.", "OK");
                return;
            }

            IsBusy = true;
            OnPropertyChanged(nameof(CanGoNext)); // deshabilita "Siguiente" mientras se envía, evita doble envío

            int? edad = int.TryParse(UsuarioGlobal.Age, out var edadValor) ? edadValor : null;
            int? estatura = int.TryParse(UsuarioGlobal.Stature, out var estaturaValor) ? estaturaValor : null;

            var resultado = await ApiClient.PostAsync<EvaluacionResponseDto>("/api/evaluaciones", new
            {
                visitanteId = UsuarioGlobal.VisitanteId,
                edad,
                estatura,
                opcionesRespuestaIds,
            });

            IsBusy = false;
            OnPropertyChanged(nameof(CanGoNext));

            if (!resultado.Success)
            {
                await Application.Current.MainPage.DisplayAlert("Error", resultado.ErrorMessage ?? "No se pudo procesar la evaluación.", "OK");
                return;
            }

            await Application.Current.MainPage.Navigation.PushAsync(new ValidacionAtraccionesView(resultado.Data));
        }

        private void PreviousQuestion()
        {
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                UpdateBindings();
            }
        }

        private void UpdateBindings()
        {
            OnPropertyChanged(nameof(CurrentQuestionText));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsLastQuestion));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));

            CurrentOptions.Clear();
            if (questions.Count > 0)
                foreach (var opt in questions[currentQuestionIndex].Options)
                    CurrentOptions.Add(opt);
        }
    }
}
