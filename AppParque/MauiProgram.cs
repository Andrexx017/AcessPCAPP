using Microsoft.Extensions.Logging;
using PdfSharpCore.Fonts;
using AppParque.Services;

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

        return builder.Build();
    }
}
