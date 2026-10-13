using AppParque.Features.Asistente;

namespace AppParque.Shared;

public partial class AsistenteFlotanteView : ContentView
{
    private bool _panelAbierto;

    public AsistenteFlotanteView()
    {
        InitializeComponent();
    }

    private async void OnTapped(object sender, TappedEventArgs e)
    {
        // En celular la pantalla ya es chica, así que el chat se abre a pantalla completa como
        // antes. En escritorio/tablet se abre como un panel pequeño junto al botón, para no tapar
        // el resto de la app con cada pregunta.
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            await Navigation.PushAsync(new AsistenteChatView());
            return;
        }

        panelChat.Content ??= new AsistenteChatContentView();

        _panelAbierto = !_panelAbierto;
        panelChat.IsVisible = _panelAbierto;
        iconoBurbuja.Text = _panelAbierto ? "" : "";
    }
}
