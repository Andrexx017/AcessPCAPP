using Microsoft.Extensions.Logging;
using PdfSharpCore.Fonts;
using AppParque.Services;
#if WINDOWS
using Microsoft.Maui.Handlers;
using WThickness = Microsoft.UI.Xaml.Thickness;
using WHorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using WVerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
#endif

namespace AppParque;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Registrar el font resolver
        GlobalFontSettings.FontResolver = new CustomFontResolver();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Font Awesome 7 Brands-Regular-400.otf", "BrandsRegular");
                fonts.AddFont("Font Awesome 7 Free-Regular-400.otf", "FreeRegular");
                fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FreeSolid");
                fonts.AddFont("ARIAL.TTF", "Arial");
                fonts.AddFont("ARIALBD.TTF", "ArialBold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if WINDOWS
        // El ComboBox nativo de WinUI (lo que usa Picker en Windows) no pinta su fondo/borde en reposo
        // desde las propiedades Background/BorderBrush: usa recursos de tema por estado
        // (ComboBoxBackground, ComboBoxBorderBrush, etc). Sin esto, el selector se ve como una simple
        // línea delgada en vez de llenar la tarjeta (Border) que lo envuelve en el XAML, y casi
        // invisible en modo claro. Lo forzamos transparente en todos los estados para que se vea el
        // Border/tarjeta que ya está pintado correctamente en ambos temas.
        PickerHandler.Mapper.AppendToMapping("SinCromadoNativo", (handler, _) =>
        {
            var comboBox = handler.PlatformView;
            var transparente = new WSolidColorBrush(Microsoft.UI.Colors.Transparent);

            comboBox.BorderThickness = new WThickness(0);
            comboBox.HorizontalAlignment = WHorizontalAlignment.Stretch;
            comboBox.VerticalAlignment = WVerticalAlignment.Stretch;
            comboBox.HorizontalContentAlignment = WHorizontalAlignment.Stretch;

            foreach (var clave in new[]
            {
                "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed",
                "ComboBoxBackgroundDisabled", "ComboBoxBackgroundUnfocused",
                "ComboBoxBorderBrush", "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed",
                "ComboBoxBorderBrushDisabled", "ComboBoxBorderBrushUnfocused",
            })
            {
                comboBox.Resources[clave] = transparente;
            }
        });
#endif

        return builder.Build();
    }
}
