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
            var archivos = await _fileSaverService.ListPdfsAsync();
            SinPdfsLayout.IsVisible = archivos.Count == 0;
            ListaPdfs.ItemsSource   = archivos.Count > 0 ? archivos : null;
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
