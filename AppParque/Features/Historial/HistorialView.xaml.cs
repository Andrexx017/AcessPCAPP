namespace AppParque.Features.Historial
{
    public partial class HistorialView : ContentPage
    {
        private readonly HistorialViewModel _viewModel = new();

        // Mismo umbral y mecanismo que Features/Auth/LoginView.xaml.cs: MAUI no tiene media queries,
        // así que el cambio entre barra lateral fija (ancho) y panel colapsable (angosto) se hace a mano.
        private const double WideBreakpoint = 800;
        private bool? _isWide;
        private bool _filtrosAbiertos;

        public HistorialView()
        {
            InitializeComponent();
            BindingContext = _viewModel;

            dateDesde.Date = DateTime.Today.AddDays(-30);
            dateHasta.Date = DateTime.Today;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = _viewModel.CargarHistorialAsync();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            bool isWide = width >= WideBreakpoint;
            if (_isWide == isWide) return;
            _isWide = isWide;

            rootGrid.RowDefinitions.Clear();
            rootGrid.ColumnDefinitions.Clear();

            if (isWide)
            {
                rootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(280)));
                rootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetRow(filterPanel, 0);
                Grid.SetColumn(filterPanel, 0);
                Grid.SetRow(mainPanel, 0);
                Grid.SetColumn(mainPanel, 1);

                filterPanel.IsVisible = true;
                btnToggleFiltros.IsVisible = false;
            }
            else
            {
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                rootGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                Grid.SetRow(filterPanel, 0);
                Grid.SetColumn(filterPanel, 0);
                Grid.SetRow(mainPanel, 1);
                Grid.SetColumn(mainPanel, 0);

                filterPanel.IsVisible = _filtrosAbiertos;
                btnToggleFiltros.IsVisible = true;
            }
        }

        private void OnToggleFiltrosClicked(object sender, EventArgs e)
        {
            _filtrosAbiertos = !_filtrosAbiertos;
            filterPanel.IsVisible = _filtrosAbiertos;
        }

        private void OnAplicarFiltrosClicked(object sender, EventArgs e)
        {
            _viewModel.Query = entryBuscar.Text ?? "";
            _viewModel.FiltrarPorFecha = chkFiltrarFecha.IsChecked;
            _viewModel.FechaDesde = dateDesde.Date;
            _viewModel.FechaHasta = dateHasta.Date;
            _viewModel.EnfermeroSeleccionado = pickerEnfermero.SelectedItem as string ?? "Todos";

            _viewModel.AplicarFiltros();

            if (_isWide == false)
            {
                _filtrosAbiertos = false;
                filterPanel.IsVisible = false;
            }
        }

        private void OnLimpiarFiltrosClicked(object sender, EventArgs e)
        {
            entryBuscar.Text = "";
            chkFiltrarFecha.IsChecked = false;
            dateDesde.Date = DateTime.Today.AddDays(-30);
            dateHasta.Date = DateTime.Today;
            pickerEnfermero.SelectedIndex = 0;

            _viewModel.LimpiarFiltros();
        }

        private async void OnExportarCsvClicked(object sender, EventArgs e)
        {
            await _viewModel.ExportarCsvAsync();
        }

        private async void OnExportarExcelClicked(object sender, EventArgs e)
        {
            await _viewModel.ExportarExcelAsync();
        }
    }
}
