namespace TFG_APPBusinessIntelligence.Views
{
    public partial class SesionExpiradaPopup : ContentPage
    {
        private readonly TaskCompletionSource<bool> _tcs = new();

        public Task Resultado => _tcs.Task;

        public SesionExpiradaPopup()
        {
            InitializeComponent();
        }

        private void OnAceptarClicked(object? sender, EventArgs e) => Cerrar();

        private void OnFondoTapped(object? sender, TappedEventArgs e) { /* bloquear cierre accidental */ }

        private void Cerrar()
        {
            _tcs.TrySetResult(true);
            Navigation.PopModalAsync();
        }
    }
}
