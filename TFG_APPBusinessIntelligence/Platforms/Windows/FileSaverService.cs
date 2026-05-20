namespace TFG_APPBusinessIntelligence.Services
{
    public class FileSaverService : IFileSaverService
    {
        private string CarpetaDestino =>
            Preferences.Default.Get("carpeta_informes", FileSystem.CacheDirectory);

        // ── Guardar PDF ───────────────────────────────────────────────────────
        public Task<string> SavePdfAsync(string tempPath, string fileName)
        {
            var dir = CarpetaDestino;
            if (!Directory.Exists(dir)) dir = FileSystem.CacheDirectory;

            string destPath = Path.Combine(dir, fileName);
            File.Copy(tempPath, destPath, overwrite: true);
            return Task.FromResult(destPath);
        }

        // ── Listar PDFs ───────────────────────────────────────────────────────
        public Task<List<PdfEntry>> ListPdfsAsync()
        {
            var dir = CarpetaDestino;
            if (!Directory.Exists(dir))
                return Task.FromResult(new List<PdfEntry>());

            var lista = Directory.GetFiles(dir, "*.pdf")
                .OrderByDescending(File.GetLastWriteTime)
                .Select(f =>
                {
                    var info = new FileInfo(f);
                    return new PdfEntry
                    {
                        Nombre  = info.Name,
                        Detalle = $"{info.LastWriteTime:dd/MM/yyyy HH:mm}  ·  {info.Length / 1024.0:F1} KB",
                        Token   = f
                    };
                })
                .ToList();

            return Task.FromResult(lista);
        }

        // ── Abrir PDF ─────────────────────────────────────────────────────────
        public async Task OpenPdfAsync(string token)
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(token)
            });
        }

        // ── Eliminar PDF ──────────────────────────────────────────────────────
        public Task<bool> DeletePdfAsync(string token)
        {
            try
            {
                if (File.Exists(token))
                {
                    File.Delete(token);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
