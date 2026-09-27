using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AppParque.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Text.Json.Serialization;
using AppParque.Shared;
using AppParque.Shared.Models;
using AppParque.Features.ValidacionAtracciones;

namespace AppParque.Features.EvaluacionAccesibilidad
{
    public class TestViewModel : BaseViewModel
    {
        private readonly PdfGeneratorService _pdfGeneratorService;
        private readonly FireBaseService _firebaseService;

        public string Estatura => UsuarioGlobal.Stature;

        private int currentQuestionIndex = 0;
        private List<Question> questions;

        public ObservableCollection<string> CurrentOptions { get; set; } = new();
        public ObservableCollection<Attraction> AtraccionesPreseleccionadas { get; } = new();

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand GenerarPDFCommand { get; }

        private bool _isGeneratingPDF;
        public bool IsGeneratingPDF
        {
            get => _isGeneratingPDF;
            set
            {
                if (_isGeneratingPDF != value)
                {
                    _isGeneratingPDF = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotGeneratingPDF));
                }
            }
        }
        public bool IsNotGeneratingPDF => !IsGeneratingPDF;

        public TestViewModel()
        {
            _pdfGeneratorService = new PdfGeneratorService();
            _firebaseService = new FireBaseService();

            questions = new List<Question>
            {
                new Question {
                    Text = "¿Puede caminar sin ningún tipo de apoyo como bastón, muletas o prótesis?",
                    Options = new List<string> {"Sí", "No"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene prótesis en las piernas?",
                    Options = new List<string> {"No", "Sí, por debajo de rodilla", "Sí, por encima de rodilla", "Ambas"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Puede subir y bajar escaleras sin ayuda?",
                    Options = new List<string> {"Sí", "Sí, con ayuda (bastón/muleta)", "No" },
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene capacidad de agarre en ambas manos?",
                    Options = new List<string> {"Sí", "Solo una mano", "Ninguna"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene alguna ausencia o prótesis de brazo o mano?",
                    Options = new List<string> {"No", "Prótesis en una mano", "Prótesis en ambas manos" },
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene discapacidad visual total?",
                    Options = new List<string> {"Sí", "No", "Parcial"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene discapacidad auditiva total?",
                    Options = new List<string> {"Sí", "No"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene alguna condición cognitiva diagnosticada o dificultad para entender instrucciones complejas?",
                    Options = new List<string> {"Sí", "No"},
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene una discapacidad certificada por alteración del sistema nervioso o trastornos mentales severos?",
                    Options = new List<string> {"Sí", "No" },
                    IsMultipleChoice = false
                },
                new Question {
                    Text = "¿Tiene actualmente alguna de las siguientes condiciones médicas? (Marca todas las que apliquen)",
                    Options = new List<string>
                    {
                        "Embarazo",
                        "Problemas cardíacos, presión alta, marcapasos",
                        "Cirugías recientes, yesos o lesiones",
                        "Problemas de cuello, columna o huesos",
                        "Mareo, vértigo o miedo a las alturas",
                        "Miedo a espacios cerrados",
                        "Ninguna"
                    },
                    IsMultipleChoice = true
                },
            };


            CurrentOptions.Clear();
            foreach (var o in questions[0].Options)
                CurrentOptions.Add(o);

            NextCommand = new Command(async () => await NextQuestionAsync());
            PreviousCommand = new Command(PreviousQuestion);
            GenerarPDFCommand = new Command(async () => await GenerarPDFAsync());
        }

        public string CurrentQuestionText => questions[currentQuestionIndex].Text;
        public string ProgressText => $"Pregunta {currentQuestionIndex + 1} de {questions.Count}";
        public double Progress => (double)(currentQuestionIndex + 1) / questions.Count;
        public bool IsLastQuestion => currentQuestionIndex == questions.Count - 1;
        public bool CanGoBack => currentQuestionIndex > 0;
        public bool CanGoNext => true;

        public Question GetCurrentQuestion()
        {
            if (currentQuestionIndex >= 0 && currentQuestionIndex < questions.Count)
                return questions[currentQuestionIndex];
            return null;
        }

        private async Task NextQuestionAsync()
        {
            if (currentQuestionIndex < questions.Count - 1)
            {
                currentQuestionIndex++;
                UpdateBindings();
                return;
            }

            if (IsLastQuestion)
            {
                var restriccionesUsuario = MapAnswersToRestrictions();

                // 💾 guardar respuestas en Firebase
                await GuardarResultadoTestAsync();

                int est = int.TryParse(Estatura, out var r) ? r : 0;
                await Application.Current.MainPage.Navigation.PushAsync(new ValidacionAtraccionesView(restriccionesUsuario, est));
            }
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
            foreach (var opt in questions[currentQuestionIndex].Options)
                CurrentOptions.Add(opt);
        }

        private async Task GenerarPDFAsync()
        {
            try
            {
                IsGeneratingPDF = true;
                var path = System.IO.Path.Combine(FileSystem.AppDataDirectory, $"Atracciones_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                if (AtraccionesPreseleccionadas.Count > 0)
                {
                    await _pdfGeneratorService.GenerateAttractionsPdfFromListAsync(path, AtraccionesPreseleccionadas);
                }
                else
                {
                    var restricciones = MapAnswersToRestrictions();
                    int est = int.TryParse(Estatura, out var r) ? r : 0;
                    await _pdfGeneratorService.GenerateAttractionsPdfAsync(path, restricciones, est);
                }

                IsGeneratingPDF = false;

                bool abrir = await Application.Current.MainPage.DisplayAlert("✅ PDF generado", "El archivo se guardó correctamente", "Abrir", "OK");
                if (abrir)
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
                }
            }
            catch (Exception ex)
            {
                IsGeneratingPDF = false;
                await Application.Current.MainPage.DisplayAlert("❌ Error", $"No se pudo generar el PDF: {ex.Message}", "OK");
            }
        }

        // 🚀 Mapeo de respuestas (tomado de tu V1.0)
        private Restrictions MapAnswersToRestrictions()
        {
            var restrictions = new Restrictions();

            restrictions.grupos = new Dictionary<string, bool>
    {
        { "grupo_1", false },
        { "grupo_1_1", false },
        { "grupo_2", false },
        { "grupo_3", false },
        { "grupo_4", false },
        { "grupo_5", false },
        { "grupo_5_1", false },
        { "grupo_5_2", false },
        { "grupo_10", false },
        { "protesis_mano", false },
        { "protesis_pierna_arriba_rodilla", false }
    };

            restrictions.condiciones = new Dictionary<string, bool>
    {
        { "discapacidad_visual", false },
        { "discapacidad_auditiva", false },
        { "discapacidad_cognitiva", false },
        { "embarazo", false },
        { "problemas_cardiacos", false },
        { "problemas_columna", false },
        { "mareo_vertigo", false },
        { "miedo_espacios_cerrados", false }
    };

            // --- Preguntas base ---
            bool caminaSinApoyo = questions[0].SelectedAnswers?.Contains("Sí") ?? false;
            bool noCamina = questions[0].SelectedAnswers?.Contains("No") ?? false;

            bool subeSinAyuda = questions[2].SelectedAnswers?.Contains("Sí") ?? false;
            bool subeConAyuda = questions[2].SelectedAnswers?.Contains("Sí, con ayuda (bastón/muleta)") ?? false;
            bool noSube = questions[2].SelectedAnswers?.Contains("No") ?? false;

            bool agarreAmbas = questions[3].SelectedAnswers?.Contains("Sí") ?? false;
            bool agarreUna = questions[3].SelectedAnswers?.Contains("Solo una mano") ?? false;
            bool sinAgarre = questions[3].SelectedAnswers?.Contains("Ninguna") ?? false;

            bool protesisDebajoRodilla = questions[1].SelectedAnswers?.Contains("Sí, por debajo de rodilla") ?? false;
            bool protesisArribaRodilla = questions[1].SelectedAnswers?.Contains("Sí, por encima de rodilla") ?? false;
            bool protesisAmbasPiernas = questions[1].SelectedAnswers?.Contains("Ambas") ?? false;
            bool protesisManoUna = questions[4].SelectedAnswers?.Contains("Prótesis en una mano") ?? false;
            bool protesisManoAmbas = questions[4].SelectedAnswers?.Contains("Prótesis en ambas manos") ?? false;

            // --- Reglas de grupos ---
            if (noCamina) restrictions.grupos["grupo_1"] = true;
            if (caminaSinApoyo && subeSinAyuda && sinAgarre) restrictions.grupos["grupo_1_1"] = true;
            if ((caminaSinApoyo || subeConAyuda) && agarreUna) restrictions.grupos["grupo_2"] = true;
            if ((caminaSinApoyo || subeConAyuda) && agarreUna) restrictions.grupos["grupo_3"] = true;
            if (subeConAyuda && agarreAmbas) restrictions.grupos["grupo_4"] = true;
            if (caminaSinApoyo && subeSinAyuda && agarreUna && !protesisDebajoRodilla && !protesisArribaRodilla) restrictions.grupos["grupo_5"] = true;
            if (caminaSinApoyo && subeSinAyuda && agarreAmbas && (protesisDebajoRodilla || protesisAmbasPiernas)) restrictions.grupos["grupo_5_1"] = true;
            if (caminaSinApoyo && subeSinAyuda && (protesisArribaRodilla || (protesisDebajoRodilla && protesisArribaRodilla))) restrictions.grupos["grupo_5_2"] = true;
            if (questions[8].SelectedAnswers?.Contains("Sí") ?? false) restrictions.grupos["grupo_10"] = true;

            restrictions.grupos["protesis_mano"] = protesisManoUna || protesisManoAmbas;
            restrictions.grupos["protesis_pierna_arriba_rodilla"] = protesisArribaRodilla;

            // --- Condiciones ---
            var visual = questions[5].SelectedAnswers ?? new List<string>();
            if (visual.Contains("Sí") || visual.Contains("Parcial")) restrictions.condiciones["discapacidad_visual"] = true;

            var auditiva = questions[6].SelectedAnswers ?? new List<string>();
            if (auditiva.Contains("Sí")) restrictions.condiciones["discapacidad_auditiva"] = true;

            var cognitiva = questions[7].SelectedAnswers ?? new List<string>();
            if (cognitiva.Contains("Sí")) restrictions.condiciones["discapacidad_cognitiva"] = true;

            var selected = questions[9].SelectedAnswers ?? new List<string>();
            restrictions.condiciones["embarazo"] = selected.Contains("Embarazo");
            restrictions.condiciones["problemas_cardiacos"] = selected.Contains("Problemas cardíacos, presión alta, marcapasos");
            restrictions.condiciones["problemas_columna"] = selected.Contains("Problemas de cuello, columna o huesos");
            restrictions.condiciones["mareo_vertigo"] = selected.Contains("Mareo, vértigo o miedo a las alturas");
            restrictions.condiciones["miedo_espacios_cerrados"] = selected.Contains("Miedo a espacios cerrados");

            // 🐞 DEBUG
            int est = int.TryParse(Estatura, out var r) ? r : 0;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Estatura del usuario: {Estatura} (parseado: {est})");

            System.Diagnostics.Debug.WriteLine("===== MAPEO DE GRUPOS =====");
            foreach (var g in restrictions.grupos)
                System.Diagnostics.Debug.WriteLine($"Grupo: {g.Key} => {g.Value}");

            System.Diagnostics.Debug.WriteLine("===== MAPEO DE CONDICIONES =====");
            foreach (var c in restrictions.condiciones)
                System.Diagnostics.Debug.WriteLine($"Condición: {c.Key} => {c.Value}");

            return restrictions;
        }


        private async Task GuardarResultadoTestAsync()
        {
            try
            {
                var respuestas = new List<string>();
                foreach (var q in questions)
                {
                    if (q.SelectedAnswers != null && q.SelectedAnswers.Count > 0)
                        respuestas.Add(string.Join(",", q.SelectedAnswers));
                    else
                        respuestas.Add("Sin respuesta");
                }

                var respuestasUnidas = string.Join(" | ", respuestas);

                var test = new DataUser
                {
                    name = UsuarioGlobal.Name,
                    typeId = UsuarioGlobal.TypeId,
                    idNumber = UsuarioGlobal.IdNumber,
                    age = UsuarioGlobal.Age,
                    stature = UsuarioGlobal.Stature,
                    //nurseName = UsuarioGlobal.Username,
                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    answers = respuestasUnidas
                };

                var result = await _firebaseService.AddTestAsync(UsuarioGlobal.Uid, test);

                if (!result)
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo guardar el resultado en Firebase", "OK");
                else
                    await Application.Current.MainPage.DisplayAlert("Éxito", "El test fue guardado correctamente", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Excepción al guardar test: {ex.Message}", "OK");
            }
        }
    }
}
