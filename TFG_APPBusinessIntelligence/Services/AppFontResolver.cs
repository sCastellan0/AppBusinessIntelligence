using PdfSharpCore.Fonts;
using System.Reflection;

namespace TFG_APPBusinessIntelligence.Services
{
    /// <summary>
    /// FontResolver personalizado para PdfSharpCore en Android/MAUI.
    /// Carga las fuentes desde los recursos embebidos del ensamblado,
    /// ya que Android no permite acceder al sistema de archivos de fuentes del SO.
    /// </summary>
    public class AppFontResolver : IFontResolver
    {
        public static readonly AppFontResolver Instance = new();

        private const string FuenteRegular  = "OpenSans-Regular";
        private const string FuenteNegrita  = "OpenSans-Semibold";

        // Nombre base que devolvemos siempre (PdfSharpCore lo usa como clave)
        public string DefaultFontName => FuenteRegular;

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Mapeamos cualquier familia a OpenSans según el peso
            string fuente = isBold ? FuenteNegrita : FuenteRegular;
            return new FontResolverInfo(fuente);
        }

        public byte[]? GetFont(string faceName)
        {
            string archivo = faceName == FuenteNegrita
                ? "OpenSans-Semibold.ttf"
                : "OpenSans-Regular.ttf";

            return CargarFuenteEmbebida(archivo);
        }

        private static byte[]? CargarFuenteEmbebida(string nombreArchivo)
        {
            var ensamblado = Assembly.GetExecutingAssembly();

            // Busca el recurso embebido con ese nombre de archivo
            string? nombreRecurso = ensamblado
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(nombreArchivo, StringComparison.OrdinalIgnoreCase));

            if (nombreRecurso is null)
                return null;

            using var stream = ensamblado.GetManifestResourceStream(nombreRecurso);
            if (stream is null) return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
