using Android.Content;
using Android.Database;
using Android.Provider;

namespace TFG_APPBusinessIntelligence.Services
{
    public class FileSaverService : IFileSaverService
    {
        // ── Guardar PDF ───────────────────────────────────────────────────────
        public async Task<string> SavePdfAsync(string tempPath, string fileName)
        {
            var uriStr = Preferences.Default.Get("carpeta_informes_uri", string.Empty);

            if (!string.IsNullOrEmpty(uriStr))
            {
                var treeUri  = Android.Net.Uri.Parse(uriStr)!;
                var context  = Platform.AppContext;
                var resolver = context.ContentResolver!;

                var treeDocId  = DocumentsContract.GetTreeDocumentId(treeUri)
                    ?? throw new Exception("No se pudo obtener el ID del árbol SAF.");
                var rootDocUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, treeDocId)!;

                // Borrar fichero anterior con el mismo nombre si existe
                EliminarSiExiste(resolver, treeUri, treeDocId, fileName);

                var newUri = DocumentsContract.CreateDocument(resolver, rootDocUri, "application/pdf", fileName)
                    ?? throw new Exception("No se pudo crear el documento en la carpeta seleccionada.");

                using (var outStream = resolver.OpenOutputStream(newUri, "wt")
                    ?? throw new Exception("No se pudo abrir el stream de escritura."))
                using (var inStream = File.OpenRead(tempPath))
                {
                    await inStream.CopyToAsync(outStream);
                }

                var carpeta = Preferences.Default.Get("carpeta_informes", string.Empty);
                return string.IsNullOrEmpty(carpeta)
                    ? newUri.ToString()!
                    : Path.Combine(carpeta, fileName);
            }

            // Fallback: caché interna
            string destPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.Copy(tempPath, destPath, overwrite: true);
            return destPath;
        }

        // ── Listar PDFs ───────────────────────────────────────────────────────
        public Task<List<PdfEntry>> ListPdfsAsync()
        {
            var lista = new List<PdfEntry>();
            var uriStr = Preferences.Default.Get("carpeta_informes_uri", string.Empty);

            if (!string.IsNullOrEmpty(uriStr))
            {
                var treeUri    = Android.Net.Uri.Parse(uriStr)!;
                var context    = Platform.AppContext;
                var resolver   = context.ContentResolver!;
                var treeDocId  = DocumentsContract.GetTreeDocumentId(treeUri);
                if (treeDocId != null)
                {
                    var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, treeDocId)!;
                    ICursor? cursor = null;
                    try
                    {
                        cursor = resolver.Query(childrenUri,
                            new[]
                            {
                                DocumentsContract.Document.ColumnDocumentId,
                                DocumentsContract.Document.ColumnDisplayName,
                                DocumentsContract.Document.ColumnMimeType,
                                DocumentsContract.Document.ColumnLastModified,
                                DocumentsContract.Document.ColumnSize
                            },
                            null, null, null);

                        if (cursor != null)
                        {
                            while (cursor.MoveToNext())
                            {
                                var mime = cursor.GetString(2) ?? string.Empty;
                                var name = cursor.GetString(1) ?? string.Empty;

                                // Solo PDFs
                                if (mime != "application/pdf" &&
                                    !name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                var docId    = cursor.GetString(0)!;
                                var modified = cursor.GetLong(3);
                                var size     = cursor.GetLong(4);

                                var fileUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, docId)!;

                                var date = DateTimeOffset.FromUnixTimeMilliseconds(modified).LocalDateTime;
                                var kb   = size / 1024.0;

                                lista.Add(new PdfEntry
                                {
                                    Nombre  = name,
                                    Detalle = $"{date:dd/MM/yyyy HH:mm}  ·  {kb:F1} KB",
                                    Token   = fileUri.ToString()!
                                });
                            }
                        }
                    }
                    finally
                    {
                        cursor?.Close();
                    }

                    lista = lista.OrderByDescending(x => x.Detalle).ToList();
                }
            }

            return Task.FromResult(lista);
        }

        // ── Abrir PDF ─────────────────────────────────────────────────────────
        public async Task OpenPdfAsync(string token)
        {
            var uri = Android.Net.Uri.Parse(token)!;
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, "application/pdf");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(intent);
            await Task.CompletedTask;
        }

        // ── Eliminar PDF ──────────────────────────────────────────────────────
        public Task<bool> DeletePdfAsync(string token)
        {
            try
            {
                var uri      = Android.Net.Uri.Parse(token)!;
                var resolver = Platform.AppContext.ContentResolver!;
                bool deleted = DocumentsContract.DeleteDocument(resolver, uri);
                return Task.FromResult(deleted);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static void EliminarSiExiste(ContentResolver resolver,
            Android.Net.Uri treeUri, string treeDocId, string fileName)
        {
            var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, treeDocId)!;
            ICursor? cursor = null;
            try
            {
                cursor = resolver.Query(childrenUri,
                    new[] { DocumentsContract.Document.ColumnDocumentId, DocumentsContract.Document.ColumnDisplayName },
                    null, null, null);

                if (cursor is null) return;
                while (cursor.MoveToNext())
                {
                    if (cursor.GetString(1) == fileName)
                    {
                        var existingUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, cursor.GetString(0)!)!;
                        DocumentsContract.DeleteDocument(resolver, existingUri);
                        break;
                    }
                }
            }
            finally { cursor?.Close(); }
        }
    }
}
