using Android.App;
using Android.Content;
using Android.Provider;

namespace TFG_APPBusinessIntelligence.Services
{
    public class FolderPickerService : IFolderPickerService
    {
        private TaskCompletionSource<string?>? _tcs;
        private const int RequestCode = 9001;

        public Task<string?> PickFolderAsync()
        {
            _tcs = new TaskCompletionSource<string?>();

            try
            {
                var intent = new Intent(Intent.ActionOpenDocumentTree);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

                var activity = Platform.CurrentActivity as MainActivity;

                if (activity == null)
                {
                    _tcs.TrySetException(new InvalidOperationException("No se pudo obtener la actividad actual"));
                    return _tcs.Task;
                }

                activity.StartActivityForResult(intent, RequestCode);

                // El resultado llega por OnActivityResult en MainActivity
                MainActivity.FolderPickerCallback = OnResult;
            }
            catch (Exception ex)
            {
                _tcs.TrySetException(ex);
            }

            return _tcs.Task;
        }

        private void OnResult(Android.Net.Uri? uri)
        {
            if (uri == null)
            {
                _tcs?.TrySetResult(null);
                return;
            }

            // Persistir permiso de escritura para esta URI
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FolderPickerService] Intentando tomar permisos para: {uri}");
                var flags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                var activity = Platform.CurrentActivity;

                if (activity?.ContentResolver == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FolderPickerService] ERROR: ContentResolver es null");
                }
                else
                {
                    activity.ContentResolver.TakePersistableUriPermission(uri, flags);
                    System.Diagnostics.Debug.WriteLine("[FolderPickerService] ✓ Permisos persistentes tomados exitosamente");

                    // Verificar que los permisos se tomaron
                    var persisted = activity.ContentResolver.PersistedUriPermissions;
                    System.Diagnostics.Debug.WriteLine($"[FolderPickerService] Total permisos persistentes: {persisted?.Count ?? 0}");

                    if (persisted != null)
                    {
                        foreach (var p in persisted)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - URI: {p.Uri}, Read: {p.IsReadPermission}, Write: {p.IsWritePermission}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FolderPickerService] ERROR tomando permisos: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FolderPickerService] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[FolderPickerService] Stack: {ex.StackTrace}");
            }

            // Guardar la URI en preferencias para usarla al escribir ficheros (Android 10+)
            Preferences.Default.Set("carpeta_informes_uri", uri.ToString());
            System.Diagnostics.Debug.WriteLine($"[FolderPickerService] URI guardada en preferencias: {uri}");

            // Convertir URI de SAF a ruta legible (solo para mostrar)
            string? ruta = ResolverRuta(uri);
            System.Diagnostics.Debug.WriteLine($"[FolderPickerService] Ruta resuelta: {ruta}");
            _tcs?.TrySetResult(ruta);
        }

        private static string? ResolverRuta(Android.Net.Uri uri)
        {
            try
            {
                // Para URIs de tipo "content://com.android.externalstorage.documents/tree/..."
                var docId = DocumentsContract.GetTreeDocumentId(uri);
                if (docId != null)
                {
                    var partes = docId.Split(':');
                    if (partes.Length == 2)
                    {
                        string tipo = partes[0];
                        string subRuta = partes[1];

                        if (tipo == "primary")
                            return $"/storage/emulated/0/{subRuta}".TrimEnd('/');

                        // Almacenamiento externo/SD
                        return $"/storage/{tipo}/{subRuta}".TrimEnd('/');
                    }
                }
            }
            catch { }

            // Fallback: devolver la URI como string
            return uri.ToString();
        }
    }
}
