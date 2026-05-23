using TFG_APPBusinessIntelligence.Services;
using Microsoft.Maui.ApplicationModel;

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
                ListaPdfs.ItemsSource = archivos.Count > 0 ? archivos : null;

                System.Diagnostics.Debug.WriteLine("[VerInformes] Carga completada exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VerInformes] ERROR: {ex.Message}");

                // Mostrar como si no hubiera archivos para evitar crash
                SinPdfsLayout.IsVisible = true;
                ListaPdfs.ItemsSource = null;

                // SOLUCIÓN: Usar el MainThread para lanzar la alerta de forma segura sin crashear OnAppearing
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error de acceso",
                        "Android bloquea el acceso a la raíz del teléfono por seguridad. Ve a Ajustes y selecciona una subcarpeta (ej. Documentos o Descargas).",
                        "Entendido");
                });
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
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar el informe.", "Aceptar");
                });
            }
        }

        private void OnOverlayCancelar(object sender, TappedEventArgs e)
            => _tcsEliminar?.TrySetResult(false);

        private void OnOverlayConfirmar(object sender, TappedEventArgs e)
            => _tcsEliminar?.TrySetResult(true);

        private async void OnVolverTapped(object sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}