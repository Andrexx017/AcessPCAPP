using Microsoft.Maui.Controls;

namespace AppParque.Features.Historial
{
    public partial class HistorialView : ContentPage
    {
        private HistorialViewModel _viewModel;

        public HistorialView()
        {
            InitializeComponent();
            _viewModel = new HistorialViewModel();
            BindingContext = _viewModel;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FiltrarHistorial(e.NewTextValue);
        }
    }
}
