using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class ConfigurarCarpetaDialog : ContentPage
    {
        private const string PrefCarpeta = "carpeta_informes";
        private const string PrefDialogoMostrado = "DialogoCarpetaMostrado";
        private readonly IFolderPickerService _folderPickerService;
        private TaskCompletionSource<bool> _taskCompletionSource = new();

        public ConfigurarCarpetaDialog(IFolderPickerService folderPickerService)
        {
            InitializeComponent();
            _folderPickerService = folderPickerService;
        }

        public Task<bool> MostrarAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            return _taskCompletionSource.Task;
        }

        private async void OnSeleccionarCarpetaClicked(object sender, EventArgs e)
        {
            try
            {
                string? carpeta = await _folderPickerService.PickFolderAsync();

                if (!string.IsNullOrEmpty(carpeta))
                {
                    Preferences.Default.Set(PrefCarpeta, carpeta);
                    Preferences.Default.Set(PrefDialogoMostrado, true);

                    // Mostrar diálogo personalizado de éxito
                    var notificacion = NotificacionDialog.Exito(
                        "Carpeta Configurada",
                        "Los informes se guardarán en:",
                        carpeta);

                    await Navigation.PushModalAsync(notificacion, animated: true);
                    await notificacion.MostrarAsync();

                    await CerrarDialogo(true);
                }
            }
            catch (Exception ex)
            {
                // Mostrar diálogo personalizado de error
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "No se pudo seleccionar la carpeta",
                    ex.Message);

                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
            }
        }

        private async void OnMasTardeClicked(object sender, EventArgs e)
        {
            Preferences.Default.Set(PrefDialogoMostrado, true);
            await CerrarDialogo(false);
        }

        private async Task CerrarDialogo(bool carpetaConfigurada)
        {
            await Navigation.PopModalAsync();
            _taskCompletionSource?.SetResult(carpetaConfigurada);
        }

        protected override bool OnBackButtonPressed()
        {
            // Permitir cerrar con el botón atrás (equivalente a "Más tarde")
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                Preferences.Default.Set(PrefDialogoMostrado, true);
                await CerrarDialogo(false);
            });
            return true;
        }

        // Método estático para verificar si debe mostrarse el diálogo
        public static bool DebesMostrarDialogo()
        {
            var carpetaConfigurada = !string.IsNullOrEmpty(Preferences.Default.Get(PrefCarpeta, string.Empty));
            var dialogoYaMostrado = Preferences.Default.Get(PrefDialogoMostrado, false);

            // Mostrar solo si no hay carpeta configurada Y el diálogo no se ha mostrado antes
            return !carpetaConfigurada && !dialogoYaMostrado;
        }

        // Método para resetear el flag (útil para testing o si quieres volver a mostrar)
        public static void ResetearDialogo()
        {
            Preferences.Default.Remove(PrefDialogoMostrado);
        }
    }
}
