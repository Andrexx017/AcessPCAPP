using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using AppParque.Shared;
using AppParque.Shared.Models;
using SizeFPdf = Syncfusion.Drawing.SizeF;
using System.Reflection;
using CloudinaryDotNet;

namespace AppParque.Services
{
    public class PdfGeneratorService
    {
        private readonly FireBaseService _firebaseService;
        private static readonly HttpClient _http = new HttpClient();
        private readonly Dictionary<string, byte[]> _imageCache = new(); // Cache local de imágenes

        public PdfGeneratorService()
        {
            _firebaseService = new FireBaseService();
            _http.Timeout = TimeSpan.FromSeconds(30);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PdfGenerator/1.0)");
        }

        public async Task GenerateAttractionsPdfAsync(string path, Restrictions restriccionesUsuario, int estaturaUsuario)
        {
            try
            {
                var atracciones = await _firebaseService.GetAttractionsAsync().ConfigureAwait(false);
                if (atracciones == null || atracciones.Count == 0)
                    throw new Exception("No se encontraron atracciones en Firebase.");

                var atraccionesFiltradas = FiltrarAtracciones(atracciones, restriccionesUsuario, estaturaUsuario);

                await Task.Run(() => GeneratePdfInternal(path, atraccionesFiltradas)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generando PDF: {ex.Message}");
            }
        }

        public async Task GenerateAttractionsPdfFromListAsync(string path, IEnumerable<Attraction> atraccionesSeleccionadas)
        {
            try
            {
                var dict = new Dictionary<string, Attraction>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in atraccionesSeleccionadas)
                {
                    var key = string.IsNullOrWhiteSpace(a.name) ? Guid.NewGuid().ToString() : a.name.Trim();
                    if (!dict.ContainsKey(key))
                        dict[key] = a;
                }

                await Task.Run(() => GeneratePdfInternal(path, dict)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generando PDF: {ex.Message}");
            }
        }

        private Dictionary<string, Attraction> FiltrarAtracciones(Dictionary<string, Attraction> atracciones, Restrictions restricciones, int estatura)
        {
            var filtradas = new Dictionary<string, Attraction>();

            foreach (var atr in atracciones)
            {
                bool cumple = true;

                if (atr.Value.stature_min.HasValue && estatura < atr.Value.stature_min.Value)
                    cumple = false;
                if (atr.Value.stature_max.HasValue && estatura > atr.Value.stature_max.Value)
                    cumple = false;

                if (cumple)
                    filtradas.Add(atr.Key, atr.Value);
            }

            return filtradas;
        }

        private async Task GeneratePdfInternal(string path, Dictionary<string, Attraction> atracciones)
        {
            using (var document = new PdfDocument())
            {
                var resources = await _firebaseService.GetResourcesAsync();
                PdfBitmap logoImage = null, mapaImage = null;

                if (resources != null)
                {
                    if (resources.TryGetValue("logo", out var logoUrl))
                    {
                        var logoBytes = await GetImageBytesAsync(logoUrl);
                        if (logoBytes != null && logoBytes.Length > 0)
                            logoImage = new PdfBitmap(new MemoryStream(logoBytes));
                    }
                    if (resources.TryGetValue("mapa", out var mapaUrl))
                    {
                        var mapaBytes = await GetImageBytesAsync(mapaUrl);
                        if (mapaBytes != null && mapaBytes.Length > 0)
                            mapaImage = new PdfBitmap(new MemoryStream(mapaBytes));
                    }
                }

                var colorRojo = new PdfColor(204, 0, 0);
                var colorVerde = new PdfColor(102, 204, 102);
                var colorGrisClaro = new PdfColor(240, 240, 240);

                var fontTitle = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
                var fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
                var fontBody = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
                var fontBold = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
                var fontGray = new PdfStandardFont(PdfFontFamily.Helvetica, 9);

                float topMargin = 50, leftMargin = 40, rightMargin = 40;
                float columnSpacing = 15;
                float imageWidth = 100, imageHeight = 75;
                float y = topMargin;

                void DrawLogo(PdfPage page, PdfGraphics g)
                {
                    if (logoImage != null)
                    {
                        float ratioLogo = (float)logoImage.PhysicalDimension.Width / logoImage.PhysicalDimension.Height;
                        float finalWidthLogo = 80;
                        float finalHeightLogo = finalWidthLogo / ratioLogo;
                        float xLogo = page.GetClientSize().Width - finalWidthLogo - 20;
                        g.DrawImage(logoImage, xLogo, 10, finalWidthLogo, finalHeightLogo);

                        // Marca de agua repetida en toda la página
                        float wmWidth = page.GetClientSize().Width / 3;   // tamaño de cada logo
                        float wmHeight = wmWidth / ratioLogo;

                        var wmBrush = new PdfTilingBrush(new SizeFPdf(wmWidth + 40, wmHeight + 40));
                        wmBrush.Graphics.SetTransparency(0.05f); // 5% de opacidad

                        // Dibuja el logo dentro del patrón
                        wmBrush.Graphics.DrawImage(logoImage, 20, 20, wmWidth, wmHeight);

                        // Llena toda la página con el patrón
                        g.DrawRectangle(
                            wmBrush,
                            new RectangleF(0, 0, page.GetClientSize().Width, page.GetClientSize().Height)
                        );

                    }
                }

                PdfPage page = document.Pages.Add();
                PdfGraphics graphics = page.Graphics;
                

                // Encabezado
                graphics.DrawRectangle(new PdfSolidBrush(colorGrisClaro), new RectangleF(0, 0, page.GetClientSize().Width, 80));
                DrawLogo(page, graphics);
                graphics.DrawString("Guía Personalizada de Atracciones", fontTitle, new PdfSolidBrush(colorRojo),
                    new RectangleF(leftMargin, y, page.GetClientSize().Width - leftMargin - rightMargin, 30));
                y += 30;
                graphics.DrawString("Tu seguridad y diversión son nuestra prioridad", fontHeader, new PdfSolidBrush(colorVerde),
                    new RectangleF(leftMargin, y, page.GetClientSize().Width - leftMargin - rightMargin, 20));
                y += 40;

                // Introducción con negritas en nombre y fecha
                string nombreVisitante = UsuarioGlobal.Name;
                string fechaActual = DateTime.Now.ToString("dd/MM/yyyy");
                graphics.DrawString("¡Hola ◆  ", fontBody, PdfBrushes.Black, leftMargin, y);
                float widthHola = fontBody.MeasureString("¡Hola ◆  ").Width;

                graphics.DrawString(nombreVisitante + "!  ", fontBold, PdfBrushes.Black, leftMargin + widthHola, y);
                float widthNombre = fontBold.MeasureString(nombreVisitante + "!  ").Width;

                graphics.DrawString("Hoy, ◆  ", fontBody, PdfBrushes.Black, leftMargin + widthHola + widthNombre, y);
                float widthHoy = fontBody.MeasureString("Hoy, ◆  ").Width;

                graphics.DrawString(fechaActual, fontBold, PdfBrushes.Black, leftMargin + widthHola + widthNombre + widthHoy, y);

                y += 25;

                string textoContinuacion = "Te damos la bienvenida al Parque del Café con esta Guía Personalizada de Atracciones, " +
                                           "creada especialmente para ti. En estas páginas encontrarás información para disfrutar de nuestras atracciones de manera segura y cómoda. " +
                                           "¡Recuerda que estas atracciones son las que podrás disfrutar según tus características y preferencias!";
                graphics.DrawString(textoContinuacion, fontBody, PdfBrushes.Black,
                    new RectangleF(leftMargin, y, page.GetClientSize().Width - leftMargin - rightMargin, 100),
                    new PdfStringFormat(PdfTextAlignment.Justify, PdfVerticalAlignment.Top));
                y += 110;

                // Recomendaciones
                graphics.DrawLine(new PdfPen(colorRojo, 1), leftMargin, y, page.GetClientSize().Width - rightMargin, y);
                y += 5;

                string recomendaciones = "◆ Mantente hidratado y usa protector solar.\n" +
                                         "◆ Sigue las instrucciones del personal del parque.\n" +
                                         "◆ Ante cualquier emergencia, contacta con el personal del parque.\n" +
                                         "◆ Respeta las restricciones de altura y condiciones de cada atracción.";

                // Calcular altura del bloque de texto
                float recHeight = (float)fontBody.MeasureString(
                    recomendaciones,
                    new SizeFPdf(page.GetClientSize().Width - leftMargin - rightMargin - 20, 500) // ancho disponible
                ).Height + 40; // 40px extra para el título y padding

                // Dibujar fondo del recuadro
                graphics.DrawRectangle(
                    new PdfSolidBrush(colorGrisClaro),
                    new RectangleF(leftMargin, y, page.GetClientSize().Width - leftMargin - rightMargin, recHeight)
                );

                // Título
                graphics.DrawString("Recomendaciones de Seguridad", fontHeader, new PdfSolidBrush(colorRojo),
                    new RectangleF(leftMargin + 5, y + 5, page.GetClientSize().Width, 20));

                // Texto de recomendaciones (ajustado al ancho y altura calculada)
                graphics.DrawString(
                    recomendaciones,
                    fontBody,
                    PdfBrushes.Black,
                    new RectangleF(leftMargin + 10, y + 30, page.GetClientSize().Width - leftMargin - rightMargin - 20, recHeight - 30),
                    new PdfStringFormat(PdfTextAlignment.Justify, PdfVerticalAlignment.Top)
                );

                y += recHeight + 10;


                // Atracciones con bordes redondeados
                foreach (var atr in atracciones.Values)
                {
                    float startY = y;
                    float textLeft = leftMargin + imageWidth + 20;
                    float paddingRight = 10; // espacio a la derecha
                    float textWidth = page.GetClientSize().Width - textLeft - rightMargin - paddingRight;


                    // Calcular altura de la descripción
                    float descripcionHeight = (float)fontBody.MeasureString(atr.description ?? "Sin descripción", new SizeFPdf(textWidth, 1500)).Height;

                    // Altura total de la tarjeta
                    float tarjetaHeight = Math.Max(imageHeight, descripcionHeight + 35 + 20);

                    float radius = 8f;

                    // Crear path del rectángulo redondeado
                    PdfPath path2 = new PdfPath();

                    // Esquinas y líneas del rectángulo redondeado
                    path2.AddArc(leftMargin, startY, radius * 2, radius * 2, 180, 90); // esquina superior izquierda
                    path2.AddLine(leftMargin + radius, startY, page.GetClientSize().Width - rightMargin - radius, startY); // línea superior
                    path2.AddArc(page.GetClientSize().Width - rightMargin - 2 * radius, startY, 2 * radius, 2 * radius, 270, 90); // esquina superior derecha
                    path2.AddLine(page.GetClientSize().Width - rightMargin, startY + radius, page.GetClientSize().Width - rightMargin, startY + tarjetaHeight - radius); // línea derecha
                    path2.AddArc(page.GetClientSize().Width - rightMargin - 2 * radius, startY + tarjetaHeight - 2 * radius, 2 * radius, 2 * radius, 0, 90); // esquina inferior derecha
                    path2.AddLine(page.GetClientSize().Width - rightMargin - radius, startY + tarjetaHeight, leftMargin + radius, startY + tarjetaHeight); // línea inferior
                    path2.AddArc(leftMargin, startY + tarjetaHeight - 2 * radius, 2 * radius, 2 * radius, 90, 90); // esquina inferior izquierda
                    path2.CloseFigure();

                    // Dibujar relleno y borde
                    graphics.DrawPath(new PdfSolidBrush(colorGrisClaro), path2); // relleno
                    graphics.DrawPath(new PdfPen(PdfBrushes.Gray, 1), path2); // borde

                    // Imagen
                    if (!string.IsNullOrEmpty(atr.imageURL))
                    {
                        try
                        {
                            var bytes = await GetImageBytesAsync(atr.imageURL);
                            if (bytes != null)
                            {
                                using var imgStream = new MemoryStream(bytes);
                                var img = new PdfBitmap(imgStream);
                                float ratio = (float)img.PhysicalDimension.Width / img.PhysicalDimension.Height;
                                float finalW = imageWidth;
                                float finalH = imageWidth / ratio;
                                if (finalH > imageHeight) { finalH = imageHeight * ratio; finalW = imageHeight * ratio; }
                                graphics.DrawImage(img, leftMargin + 10, startY + 10, finalW, finalH);
                            }
                        }
                        catch { }
                    }

                    // Texto
                    float ty = startY + 10;
                    graphics.DrawString(atr.name ?? "Sin nombre", fontHeader, new PdfSolidBrush(colorRojo),
                        new RectangleF(textLeft, ty, textWidth, 20));
                    ty += 20;
                    // Dibujar descripción con altura dinámica
                    graphics.DrawString(
                        atr.description ?? "Sin descripción",
                        fontBody,
                        PdfBrushes.Black,
                        new RectangleF(textLeft, ty, textWidth, descripcionHeight + 15), // darle margen extra
                        new PdfStringFormat(PdfTextAlignment.Justify, PdfVerticalAlignment.Top)
                    );
                    ty += descripcionHeight + 5;
                    string alturas = $"Altura mínima: {atr.stature_min?.ToString() ?? "N/A"} cm | Altura máxima: {atr.stature_max?.ToString() ?? "N/A"} cm";
                    graphics.DrawString(alturas, fontGray, PdfBrushes.Gray,
                        new RectangleF(textLeft, ty, textWidth, 20));

                    y += tarjetaHeight + 10;

                    if (y + tarjetaHeight > page.GetClientSize().Height - 50)
                    {
                        page = document.Pages.Add();
                        graphics = page.Graphics;
                        DrawLogo(page, graphics);
                        y = topMargin;
                    }
                }


                // Mapa y mensaje final
                if (mapaImage != null)
                {
                    page = document.Pages.Add();
                    graphics = page.Graphics;
                    DrawLogo(page, graphics);

                    float maxW = page.GetClientSize().Width - leftMargin - rightMargin;
                    float maxH = page.GetClientSize().Height - topMargin - 100;
                    float ratioMap = (float)mapaImage.PhysicalDimension.Width / mapaImage.PhysicalDimension.Height;
                    float finalWMap = Math.Min(maxW, maxH * ratioMap);
                    float finalHMap = finalWMap / ratioMap;
                    float xCentered = (page.GetClientSize().Width - finalWMap) / 2;
                    float yCentered = topMargin;

                    graphics.DrawString("🗺️ Mapa del Parque", fontHeader, new PdfSolidBrush(colorVerde), leftMargin, yCentered - 30);
                    graphics.DrawImage(mapaImage, xCentered, yCentered, finalWMap, finalHMap);

                    string finalMsg = "¡Gracias por visitar el Parque del Café!\nDisfruta tu día con seguridad y diversión.";
                    graphics.DrawString(finalMsg, fontHeader, new PdfSolidBrush(colorRojo),
                        new RectangleF(leftMargin, yCentered + finalHMap + 10, page.GetClientSize().Width - leftMargin - rightMargin, 50),
                        new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Top));
                }

                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                document.Save(fs);
            }
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