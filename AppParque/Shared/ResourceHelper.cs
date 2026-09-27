using System.IO;
using System.Reflection;

namespace AppParque.Shared
{
    public static class ResourceHelper
    {
        public static Stream GetResourceStream(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = $"AppParque.Resources.Images.{fileName}";
            var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                Console.WriteLine($"❌ Recurso no encontrado: {resourcePath}");
                throw new FileNotFoundException($"No se encontró el recurso {fileName}");
            }
            return stream;
        }

        public static byte[] GetResourceBytes(string fileName)
        {
            using var stream = GetResourceStream(fileName);
            if (stream == null) return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
