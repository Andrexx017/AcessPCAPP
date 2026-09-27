using PdfSharpCore.Fonts;
using System;
using System.IO;
using System.Reflection;

namespace AppParque.Services
{
    public class CustomFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Debug: Listar todos los recursos
                Console.WriteLine("=== RECURSOS DISPONIBLES ===");
                foreach (var resource in assembly.GetManifestResourceNames())
                {
                    Console.WriteLine(resource);
                }

                string resourceName = faceName switch
                {
                    "Arial#Bold" => "AppParque.Resources.Fonts.ARIALBD.TTF",
                    _ => "AppParque.Resources.Fonts.ARIAL.TTF"
                };

                Console.WriteLine($"Buscando: {resourceName}");

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new Exception($"No se encontró: {resourceName}");

                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        Console.WriteLine($"✓ Fuente encontrada: {resourceName}");
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return new byte[0];
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            familyName = familyName?.ToLower() ?? "";

            if (familyName.Contains("arial") || string.IsNullOrEmpty(familyName))
            {
                if (isBold)
                    return new FontResolverInfo("Arial#Bold");

                return new FontResolverInfo("Arial#Regular");
            }

            return new FontResolverInfo("Arial#Regular");
        }

        public string DefaultFontName => "Arial#Regular";
    }
}