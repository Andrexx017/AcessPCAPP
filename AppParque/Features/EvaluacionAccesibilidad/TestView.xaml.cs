namespace AppParque.Features.EvaluacionAccesibilidad;

public partial class TestView : ContentPage
{
    public TestView()
    {
        InitializeComponent();
        BindingContext = new TestViewModel();
    }

    private void OnOptionCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (BindingContext is TestViewModel vm)
        {
            var question = vm.GetCurrentQuestion();
            if (question == null) return;

            var checkbox = (CheckBox)sender;
            var optionText = (string)checkbox.BindingContext;

            if (question.IsMultipleChoice)
            {
                // ✅ Permite varias respuestas
                if (e.Value)
                {
                    if (!question.SelectedAnswers.Contains(optionText))
                        question.SelectedAnswers.Add(optionText);
                }
                else
                {
                    question.SelectedAnswers.Remove(optionText);
                }
            }
            else
            {
                // ❌ Solo una respuesta
                question.SelectedAnswers.Clear();
                if (e.Value)
                    question.SelectedAnswers.Add(optionText);

                // 🔄 Desmarcar manualmente otros checkboxes dentro del mismo StackLayout
                if (checkbox.Parent is Layout parentLayout)
                {
                    // El padre inmediato es HorizontalStackLayout
                    // Su padre es el Frame
                    if (parentLayout.Parent is Frame frame &&
                        frame.Parent is Layout container)
                    {
                        foreach (var child in container.Children)
                        {
                            if (child is Frame f &&
                                f.Content is Layout innerLayout)
                            {
                                foreach (var view in innerLayout.Children)
                                {
                                    if (view is CheckBox cb && cb != checkbox)
                                        cb.IsChecked = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }



    protected override void OnAppearing()
    {
        base.OnAppearing();
        // opcionalmente resetear flags si son necesarios
    }
}
