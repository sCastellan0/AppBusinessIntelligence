using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class VerInformes : ContentPage
    {
        private readonly IFileSaverService _fileSaverService;
        private PdfEntry? _itemPendienteEliminar;
        private TaskCompletionSource<bool>? _tcsEliminar;

        public VerInformes(IFileSaverService fileSaverService)
        {
            InitializeComponent();
            _fileSaverService = fileSaverService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPdfsAsync();
        }

        private async Task CargarPdfsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[VerInformes] Iniciando carga de PDFs...");
                var archivos = await _fileSaverService.ListPdfsAsync();
                System.Diagnostics.Debug.WriteLine($"[VerInformes] PDFs encontrados: {archivos.Count}");

                SinPdfsLayout.IsVisible = archivos.Count == 0;
                ListaPdfs.ItemsSource   = archivos.Count > 0 ? archivos : null;

                System.Diagnostics.Debug.WriteLine("[VerInformes] Carga completada exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VerInformes] ════════════ ERROR ════════════");
                System.Diagnostics.Debug.WriteLine($"[VerInformes] Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[VerInformes] Tipo: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[VerInformes] Stack trace:");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[VerInformes] Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"[VerInformes] ════════════════════════════════");

                // Mostrar como si no hubiera archivos para evitar crash
                SinPdfsLayout.IsVisible = true;
                ListaPdfs.ItemsSource = null;

                // Mostrar error detallado al usuario
                var errorDialog = NotificacionDialog.Error(
                    "Error al cargar informes",
                    "No se pudieron cargar los informes. Verifica la configuración de la carpeta.",
                    $"Detalles técnicos:\n{ex.GetType().Name}\n{ex.Message}\n\nRevisa los logs para más información.");

                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
            }
        }

        private async void OnPdfTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is PdfEntry item)
                await _fileSaverService.OpenPdfAsync(item.Token);
        }

        private async void OnEliminarPdfTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not PdfEntry item) return;

            // Mostrar overlay personalizado
            string nombreCorto = item.Nombre.Length > 40
                ? item.Nombre[..40] + "..."
                : item.Nombre;
            OverlayMensaje.Text = $"\"{nombreCorto}\"\n\nEsta accion no se puede deshacer.";
            _itemPendienteEliminar = item;
            _tcsEliminar = new TaskCompletionSource<bool>();

            OverlayEliminar.Opacity = 0;
            OverlayEliminar.IsVisible = true;
            await OverlayEliminar.FadeTo(1, 180);

            bool confirmar = await _tcsEliminar.Task;

            await OverlayEliminar.FadeTo(0, 150);
            OverlayEliminar.IsVisible = false;
            _itemPendienteEliminar = null;

            if (!confirmar) return;

            bool ok = await _fileSaverService.DeletePdfAsync(item.Token);
            if (ok)
                await CargarPdfsAsync();
            else
                await DisplayAlert("Error", "No se pudo eliminar el informe.", "Aceptar");
        }

        private void OnOverlayCancelar(object sender, TappedEventArgs e)
            => _tcsEliminar?.TrySetResult(false);

        private void OnOverlayConfirmar(object sender, TappedEventArgs e)
            => _tcsEliminar?.TrySetResult(true);

        private async void OnVolverTapped(object sender, TappedEventArgs e)
            => await Navigation.PopAsync();
    }
}
