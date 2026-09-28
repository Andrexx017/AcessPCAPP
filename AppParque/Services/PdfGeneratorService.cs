using System.Collections.Concurrent;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using AppParque.Shared;
using AppParque.Shared.Models;

namespace AppParque.Services
{
    public class PdfGeneratorService
    {
        // Imágenes públicas del parque (antes vivían en Firebase /resources.json;
        // no hay equivalente en AppParque.Api porque no son datos del dominio, son branding fijo).
        private const string LogoUrl = "https://parquedelcafe.co/wp-content/uploads/2021/05/Logo-Parque-Del-Cafe.png";
        private const string MapaUrl = "https://parquedelcafe.co/wp-content/uploads/2024/12/Mapa-web-min-scaled.jpg";

        private static readonly HttpClient _http = new HttpClient();
        // ConcurrentDictionary porque las imágenes ahora se piden en paralelo (Task.WhenAll en GeneratePdfInternal)
        private readonly ConcurrentDictionary<string, byte[]> _imageCache = new();

        public PdfGeneratorService()
        {
            // Corto a propósito: el logo y el mapa son decorativos (ver DrawLogo / sección de mapa,
            // que ya toleran imagen nula), así que una imagen caída no debe congelar la generación del PDF.
            _http.Timeout = TimeSpan.FromSeconds(8);
            if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PdfGenerator/1.0)");
        }

        public async Task GenerateAttractionsPdfFromListAsync(string path, IEnumerable<AtraccionPdfInfo> atraccionesSeleccionadas)
        {
            try
            {
                await GeneratePdfInternal(path, atraccionesSeleccionadas.ToList());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generando PDF: {ex.Message}");
            }
        }

        private async Task GeneratePdfInternal(string path, List<AtraccionPdfInfo> atracciones)
        {
            using var document = new PdfDocument();

            // Todas las imágenes remotas (logo, mapa, una por atracción) se piden en paralelo y quedan
            // cacheadas: así una imagen caída o lenta (ej. el mapa, que hoy responde con conexión reiniciada)
            // no multiplica el tiempo de espera por cada atracción dibujada más abajo.
            var urls = new[] { LogoUrl, MapaUrl }
                .Concat(atracciones.Select(a => a.ImagenUrl))
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct();
            await Task.WhenAll(urls.Select(u => GetImageBytesAsync(u)));

            XImage logoImage = null, mapaImage = null;

            var logoBytes = await GetImageBytesAsync(LogoUrl);
            if (logoBytes is { Length: > 0 })
                logoImage = XImage.FromStream(() => new MemoryStream(logoBytes));

            var mapaBytes = await GetImageBytesAsync(MapaUrl);
            if (mapaBytes is { Length: > 0 })
                mapaImage = XImage.FromStream(() => new MemoryStream(mapaBytes));

            var colorRojo = XColor.FromArgb(204, 0, 0);
            var colorVerde = XColor.FromArgb(102, 204, 102);
            var colorGrisClaro = XColor.FromArgb(240, 240, 240);

            var fontTitle = new XFont("Arial", 18, XFontStyle.Bold);
            var fontHeader = new XFont("Arial", 14, XFontStyle.Bold);
            var fontBody = new XFont("Arial", 11, XFontStyle.Regular);
            var fontBold = new XFont("Arial", 11, XFontStyle.Bold);
            var fontGray = new XFont("Arial", 9, XFontStyle.Regular);

            double topMargin = 50, leftMargin = 40, rightMargin = 40;
            double imageWidth = 100, imageHeight = 75;
            double y = topMargin;

            void DrawLogo(PdfPage p, XGraphics g)
            {
                if (logoImage is null)
                    return;

                double ratioLogo = (double)logoImage.PixelWidth / logoImage.PixelHeight;
                double finalWidthLogo = 80;
                double finalHeightLogo = finalWidthLogo / ratioLogo;
                double xLogo = p.Width.Point - finalWidthLogo - 20;
                g.DrawImage(logoImage, xLogo, 10, finalWidthLogo, finalHeightLogo);
            }

            static XTextFormatter CreateFormatter(XGraphics g) => new(g) { Alignment = XParagraphAlignment.Justify };

            var page = document.AddPage();
            var graphics = XGraphics.FromPdfPage(page);
            var formatter = CreateFormatter(graphics);

            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;

            // Encabezado
            graphics.DrawRectangle(new XSolidBrush(colorGrisClaro), 0, 0, pageWidth, 80);
            DrawLogo(page, graphics);
            graphics.DrawString("Guía Personalizada de Atracciones", fontTitle, new XSolidBrush(colorRojo),
                new XRect(leftMargin, y, pageWidth - leftMargin - rightMargin, 30), XStringFormats.TopLeft);
            y += 30;
            graphics.DrawString("Tu seguridad y diversión son nuestra prioridad", fontHeader, new XSolidBrush(colorVerde),
                new XRect(leftMargin, y, pageWidth - leftMargin - rightMargin, 20), XStringFormats.TopLeft);
            y += 40;

            // Introducción con negritas en nombre y fecha
            string nombreVisitante = UsuarioGlobal.Name;
            string fechaActual = DateTime.Now.ToString("dd/MM/yyyy");
            graphics.DrawString("¡Hola ◆  ", fontBody, XBrushes.Black, leftMargin, y);
            double widthHola = graphics.MeasureString("¡Hola ◆  ", fontBody).Width;

            graphics.DrawString(nombreVisitante + "!  ", fontBold, XBrushes.Black, leftMargin + widthHola, y);
            double widthNombre = graphics.MeasureString(nombreVisitante + "!  ", fontBold).Width;

            graphics.DrawString("Hoy, ◆  ", fontBody, XBrushes.Black, leftMargin + widthHola + widthNombre, y);
            double widthHoy = graphics.MeasureString("Hoy, ◆  ", fontBody).Width;

            graphics.DrawString(fechaActual, fontBold, XBrushes.Black, leftMargin + widthHola + widthNombre + widthHoy, y);

            y += 25;

            string textoContinuacion = "Te damos la bienvenida al Parque del Café con esta Guía Personalizada de Atracciones, " +
                                       "creada especialmente para ti. En estas páginas encontrarás información para disfrutar de nuestras atracciones de manera segura y cómoda. " +
                                       "¡Recuerda que estas atracciones son las que podrás disfrutar según tus características y preferencias!";
            formatter.DrawString(textoContinuacion, fontBody, XBrushes.Black,
                new XRect(leftMargin, y, pageWidth - leftMargin - rightMargin, 100), null);
            y += 110;

            // Recomendaciones
            graphics.DrawLine(new XPen(colorRojo, 1), leftMargin, y, pageWidth - rightMargin, y);
            y += 5;

            // Una línea por elemento (no un solo string unido con "\n"): XTextFormatter.DrawString
            // de PdfSharpCore 1.3.67 lanza NullReferenceException si el texto trae saltos de línea
            // embebidos, sin importar la fuente ni el alineado (confirmado con un repro aislado).
            string[] recomendaciones =
            {
                "◆ Mantente hidratado y usa protector solar.",
                "◆ Sigue las instrucciones del personal del parque.",
                "◆ Ante cualquier emergencia, contacta con el personal del parque.",
                "◆ Respeta las restricciones de altura y condiciones de cada atracción.",
            };

            // Calcular altura de cada línea (puede ocupar más de una línea visual si envuelve)
            double recAnchoDisponible = pageWidth - leftMargin - rightMargin - 20;
            double[] recLineHeights = recomendaciones
                .Select(linea => formatter.GetLayout(linea, fontBody, XBrushes.Black,
                    new XRect(0, 0, recAnchoDisponible, 10_000), null).Height)
                .ToArray();
            double recHeight = recLineHeights.Sum() + 40; // 40px extra para el título y padding

            // Dibujar fondo del recuadro
            graphics.DrawRectangle(new XSolidBrush(colorGrisClaro),
                new XRect(leftMargin, y, pageWidth - leftMargin - rightMargin, recHeight));

            // Título
            graphics.DrawString("Recomendaciones de Seguridad", fontHeader, new XSolidBrush(colorRojo),
                new XRect(leftMargin + 5, y + 5, pageWidth, 20), XStringFormats.TopLeft);

            // Texto de recomendaciones, una línea a la vez
            double recLineY = y + 30;
            for (int i = 0; i < recomendaciones.Length; i++)
            {
                formatter.DrawString(
                    recomendaciones[i],
                    fontBody,
                    XBrushes.Black,
                    new XRect(leftMargin + 10, recLineY, recAnchoDisponible, recLineHeights[i]),
                    null);
                recLineY += recLineHeights[i];
            }

            y += recHeight + 10;

            // Atracciones con bordes redondeados
            foreach (var atr in atracciones)
            {
                double startY = y;
                double textLeft = leftMargin + imageWidth + 20;
                double paddingRight = 10; // espacio a la derecha
                double textWidth = pageWidth - textLeft - rightMargin - paddingRight;

                // Calcular altura de la descripción
                double descripcionHeight = formatter.GetLayout(atr.Descripcion ?? "Sin descripción", fontBody, XBrushes.Black,
                    new XRect(0, 0, textWidth, 10_000), null).Height;

                // Altura total de la tarjeta
                double tarjetaHeight = Math.Max(imageHeight, descripcionHeight + 35 + 20);

                graphics.DrawRoundedRectangle(
                    new XPen(XBrushes.Gray, 1),
                    new XSolidBrush(colorGrisClaro),
                    new XRect(leftMargin, startY, pageWidth - leftMargin - rightMargin, tarjetaHeight),
                    new XSize(16, 16));

                // Imagen
                if (!string.IsNullOrEmpty(atr.ImagenUrl))
                {
                    try
                    {
                        var bytes = await GetImageBytesAsync(atr.ImagenUrl);
                        if (bytes != null)
                        {
                            using var img = XImage.FromStream(() => new MemoryStream(bytes));
                            double ratio = (double)img.PixelWidth / img.PixelHeight;
                            double finalW = imageWidth;
                            double finalH = imageWidth / ratio;
                            if (finalH > imageHeight) { finalH = imageHeight; finalW = imageHeight * ratio; }
                            graphics.DrawImage(img, leftMargin + 10, startY + 10, finalW, finalH);
                        }
                    }
                    catch { }
                }

                // Texto
                double ty = startY + 10;
                graphics.DrawString(atr.Nombre ?? "Sin nombre", fontHeader, new XSolidBrush(colorRojo),
                    new XRect(textLeft, ty, textWidth, 20), XStringFormats.TopLeft);
                ty += 20;
                // Dibujar descripción con altura dinámica
                formatter.DrawString(
                    atr.Descripcion ?? "Sin descripción",
                    fontBody,
                    XBrushes.Black,
                    new XRect(textLeft, ty, textWidth, descripcionHeight + 15), // darle margen extra
                    null);
                ty += descripcionHeight + 5;
                string alturas = $"Altura mínima: {atr.AlturaMinima?.ToString() ?? "N/A"} cm | Altura máxima: {atr.AlturaMaxima?.ToString() ?? "N/A"} cm";
                graphics.DrawString(alturas, fontGray, XBrushes.Gray,
                    new XRect(textLeft, ty, textWidth, 20), XStringFormats.TopLeft);

                y += tarjetaHeight + 10;

                if (y + tarjetaHeight > pageHeight - 50)
                {
                    page = document.AddPage();
                    graphics = XGraphics.FromPdfPage(page);
                    formatter = CreateFormatter(graphics);
                    DrawLogo(page, graphics);
                    y = topMargin;
                }
            }

            // Mapa y mensaje final
            if (mapaImage != null)
            {
                page = document.AddPage();
                graphics = XGraphics.FromPdfPage(page);
                DrawLogo(page, graphics);

                double maxW = pageWidth - leftMargin - rightMargin;
                double maxH = pageHeight - topMargin - 100;
                double ratioMap = (double)mapaImage.PixelWidth / mapaImage.PixelHeight;
                double finalWMap = Math.Min(maxW, maxH * ratioMap);
                double finalHMap = finalWMap / ratioMap;
                double xCentered = (pageWidth - finalWMap) / 2;
                double yCentered = topMargin;

                graphics.DrawString("Mapa del Parque", fontHeader, new XSolidBrush(colorVerde), leftMargin, yCentered - 30);
                graphics.DrawImage(mapaImage, xCentered, yCentered, finalWMap, finalHMap);

                string finalMsg = "¡Gracias por visitar el Parque del Café!\nDisfruta tu día con seguridad y diversión.";
                graphics.DrawString(finalMsg, fontHeader, new XSolidBrush(colorRojo),
                    new XRect(leftMargin, yCentered + finalHMap + 10, pageWidth - leftMargin - rightMargin, 50),
                    new XStringFormat { Alignment = XStringAlignment.Center });
            }

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            document.Save(fs);
        }

        private async Task<byte[]> GetImageBytesAsync(string url)
        {
            if (_imageCache.TryGetValue(url, out var cachedBytes))
                return cachedBytes;

            try
            {
                using var response = await _http.GetAsync(url).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    _imageCache[url] = bytes;
                    return bytes;
                }
            }
            catch { }
            return null;
        }
    }
}
