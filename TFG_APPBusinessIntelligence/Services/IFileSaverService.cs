namespace TFG_APPBusinessIntelligence.Services
{
    public interface IFileSaverService
    {
        /// <summary>Copia un PDF temporal a la carpeta elegida por el usuario.</summary>
        Task<string> SavePdfAsync(string tempPath, string fileName);

        /// <summary>Lista los PDFs de la carpeta configurada. Devuelve una lista de (nombre, detalle, token)
        /// donde token es lo que se necesita para abrir el archivo en esa plataforma.</summary>
        Task<List<PdfEntry>> ListPdfsAsync();

        /// <summary>Abre un PDF dado su token (ruta en Windows, URI string en Android).</summary>
        Task OpenPdfAsync(string token);

        /// <summary>Elimina un PDF dado su token.</summary>
        Task<bool> DeletePdfAsync(string token);
    }

    public class PdfEntry
    {
        public string Nombre  { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        /// <summary>Ruta de fichero (Windows) o URI SAF como string (Android).</summary>
        public string Token   { get; set; } = string.Empty;
    }
}
