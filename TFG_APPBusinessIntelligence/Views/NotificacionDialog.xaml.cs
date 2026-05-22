namespace TFG_APPBusinessIntelligence.Views
{
    public partial class NotificacionDialog : ContentPage
    {
        private TaskCompletionSource<bool> _taskCompletionSource = new();

        public NotificacionDialog()
        {
            InitializeComponent();
        }

        public static NotificacionDialog Exito(string titulo, string mensaje, string? detalle = null)
        {
            var dialog = new NotificacionDialog();
            dialog.IconoLabel.Text = "✓";
            dialog.IconoLabel.TextColor = Color.FromArgb("#10B981"); // Verde
            dialog.TituloLabel.Text = titulo;
            dialog.MensajeLabel.Text = mensaje;

            if (!string.IsNullOrEmpty(detalle))
            {
                dialog.DetalleFrame.IsVisible = true;
                dialog.DetalleLabel.Text = detalle;
            }

            return dialog;
        }

        public static NotificacionDialog Info(string titulo, string mensaje, string? detalle = null)
        {
            var dialog = new NotificacionDialog();
            dialog.IconoLabel.Text = "ℹ";
            dialog.IconoLabel.TextColor = Color.FromArgb("#3B82F6"); // Azul
            dialog.TituloLabel.Text = titulo;
            dialog.MensajeLabel.Text = mensaje;

            if (!string.IsNullOrEmpty(detalle))
            {
                dialog.DetalleFrame.IsVisible = true;
                dialog.DetalleLabel.Text = detalle;
            }

            return dialog;
        }

        public static NotificacionDialog Advertencia(string titulo, string mensaje, string? detalle = null)
        {
            var dialog = new NotificacionDialog();
            dialog.IconoLabel.Text = "⚠";
            dialog.IconoLabel.TextColor = Color.FromArgb("#F59E0B"); // Amarillo/Naranja
            dialog.TituloLabel.Text = titulo;
            dialog.MensajeLabel.Text = mensaje;

            if (!string.IsNullOrEmpty(detalle))
            {
                dialog.DetalleFrame.IsVisible = true;
                dialog.DetalleLabel.Text = detalle;
            }

            return dialog;
        }

        public static NotificacionDialog Error(string titulo, string mensaje, string? detalle = null)
        {
            var dialog = new NotificacionDialog();
            dialog.IconoLabel.Text = "✕";
            dialog.IconoLabel.TextColor = Color.FromArgb("#EF4444"); // Rojo
            dialog.TituloLabel.Text = titulo;
            dialog.MensajeLabel.Text = mensaje;

            if (!string.IsNullOrEmpty(detalle))
            {
                dialog.DetalleFrame.IsVisible = true;
                dialog.DetalleLabel.Text = detalle;
            }

            return dialog;
        }

        public Task<bool> MostrarAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            return _taskCompletionSource.Task;
        }

        private async void OnAceptarClicked(object sender, EventArgs e)
        {
            await CerrarDialogo();
        }

        private async Task CerrarDialogo()
        {
            await Navigation.PopModalAsync(animated: true);
            _taskCompletionSource?.SetResult(true);
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await CerrarDialogo();
            });
            return true;
        }
    }
}
