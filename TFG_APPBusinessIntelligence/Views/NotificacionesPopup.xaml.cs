namespace TFG_APPBusinessIntelligence.Views
{
    public partial class NotificacionesPopup : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();

        public Task<bool> Resultado => _tcs.Task;

        public NotificacionesPopup()
        {
            InitializeComponent();
        }

        private async void OnAbrirAjustesClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelarClicked(object? sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }
    }
}
