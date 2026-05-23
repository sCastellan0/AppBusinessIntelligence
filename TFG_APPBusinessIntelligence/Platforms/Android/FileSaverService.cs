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
                var treeUri  = Android.Net.Uri.Parse(uriStr);
                if (treeUri == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FileSaverService] URI de carpeta inválido al guardar");
                    throw new Exception("URI de carpeta inválido");
                }

                var context  = Platform.AppContext;

                // Verificar y tomar permisos persistentes si es necesario
                if (!VerificarPermisoPersistente(context, treeUri))
                {
                    System.Diagnostics.Debug.WriteLine("[FileSaverService] No hay permiso persistente al guardar, intentando tomarlo...");
                    if (!TomarPermisoPersistente(context, treeUri))
                    {
                        System.Diagnostics.Debug.WriteLine("[FileSaverService] ERROR: No se pudo tomar el permiso persistente para guardar");
                        throw new Exception("No se tienen permisos para guardar en la carpeta seleccionada. Por favor, vuelve a seleccionar la carpeta en Ajustes.");
                    }
                }

                var resolver = context.ContentResolver;
                if (resolver == null)
                {
                    throw new Exception("No se pudo obtener ContentResolver");
                }

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

            try
            {
                var uriStr = Preferences.Default.Get("carpeta_informes_uri", string.Empty);
                if (string.IsNullOrEmpty(uriStr))
                    return Task.FromResult(lista);

                var treeUri = Android.Net.Uri.Parse(uriStr);
                if (treeUri == null)
                    return Task.FromResult(lista);

                var context = Platform.AppContext;
                var resolver = context.ContentResolver;
                if (resolver == null)
                    return Task.FromResult(lista);

                // Verificar permisos persistentes
                if (!VerificarPermisoPersistente(context, treeUri))
                {
                    if (!TomarPermisoPersistente(context, treeUri))
                    {
                        Preferences.Default.Remove("carpeta_informes_uri");
                        Preferences.Default.Remove("carpeta_informes");
                        return Task.FromResult(lista);
                    }
                }

                var treeDocId = DocumentsContract.GetTreeDocumentId(treeUri);
                if (treeDocId == null)
                    return Task.FromResult(lista);

                var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, treeDocId);
                if (childrenUri == null)
                    return Task.FromResult(lista);

                using var cursor = resolver.Query(childrenUri,
                    new[]
                    {
                DocumentsContract.Document.ColumnDocumentId,
                DocumentsContract.Document.ColumnDisplayName,
                DocumentsContract.Document.ColumnMimeType,
                DocumentsContract.Document.ColumnLastModified,
                DocumentsContract.Document.ColumnSize
                    },
                    null, null, null);

                if (cursor == null)
                    return Task.FromResult(lista);

                while (cursor.MoveToNext())
                {
                    var mime = cursor.GetString(2) ?? "";
                    var name = cursor.GetString(1) ?? "";

                    if (mime != "application/pdf" &&
                        !name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var docId = cursor.GetString(0);
                    if (docId == null) continue;

                    var fileUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, docId);
                    if (fileUri == null) continue;

                    var modified = cursor.GetLong(3);
                    var size = cursor.GetLong(4);

                    var date = DateTimeOffset.FromUnixTimeMilliseconds(modified).LocalDateTime;
                    var kb = size / 1024.0;

                    lista.Add(new PdfEntry
                    {
                        Nombre = name,
                        Detalle = $"{date:dd/MM/yyyy HH:mm}  ·  {kb:F1} KB",
                        Token = fileUri.ToString()
                    });
                }

                return Task.FromResult(lista.OrderByDescending(x => x.Detalle).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("════════ ERROR EN ListPdfsAsync ════════");
                System.Diagnostics.Debug.WriteLine(ex.GetType().Name);
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine("INNER: " + ex.InnerException.Message);
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════");

                // IMPORTANTE: re-lanzar para que lo capture CargarPdfsAsync
                throw;
            }
        }
        // ── Verificar Permiso Persistente ─────────────────────────────────────
        private bool VerificarPermisoPersistente(Context context, Android.Net.Uri uri)
        {
            try
            {
                var persistedUris = context.ContentResolver?.PersistedUriPermissions;
                if (persistedUris == null) return false;

                foreach (var permission in persistedUris)
                {
                    if (permission.Uri?.ToString() == uri.ToString() && permission.IsReadPermission)
                    {
                        System.Diagnostics.Debug.WriteLine("[FileSaverService] Permiso persistente encontrado y válido");
                        return true;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[FileSaverService] No se encontró permiso persistente para el URI");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileSaverService] Error verificando permiso: {ex.Message}");
                return false;
            }
        }

        // ── Tomar Permiso Persistente ────────────────────────────────────────
        private bool TomarPermisoPersistente(Context context, Android.Net.Uri uri)
        {
            try
            {
                var flags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                context.ContentResolver?.TakePersistableUriPermission(uri, flags);
                System.Diagnostics.Debug.WriteLine("[FileSaverService] Permiso persistente tomado correctamente");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileSaverService] Error tomando permiso: {ex.Message}");
                return false;
            }
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
