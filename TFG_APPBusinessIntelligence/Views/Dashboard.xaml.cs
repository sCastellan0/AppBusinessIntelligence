using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Dashboard : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly DatabaseService _databaseService;
        private readonly TotpService _totpService;
        private readonly ThemeService _themeService;

        public Dashboard(FirebaseAuthService firebaseAuthService, DatabaseService databaseService, TotpService totpService, ThemeService themeService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _databaseService = databaseService;
            _totpService = totpService;
            _themeService = themeService;
        }

        private async void OnConfiguracionTapped(object? sender, EventArgs e)
        {
            var ajustes = new Ajustes(_firebaseAuthService, _databaseService, _totpService, _themeService);
            await Navigation.PushAsync(ajustes);
        }

        private async void OnSalirTapped(object? sender, EventArgs e)
        {
            await MostrarPanelSalirAsync();
        }

        private async Task MostrarPanelSalirAsync()
        {
            PanelSalir.IsVisible = true;
            await PanelSalir.TranslateTo(0, 0, 280, Easing.CubicOut);
        }

        private async Task OcultarPanelSalirAsync()
        {
            await PanelSalir.TranslateTo(0, 300, 220, Easing.CubicIn);
            PanelSalir.IsVisible = false;
        }

        private async void OnCancelarSalirTapped(object? sender, TappedEventArgs e)
        {
            await OcultarPanelSalirAsync();
        }

        private async void OnConfirmarSalirTapped(object? sender, TappedEventArgs e)
        {
            await OcultarPanelSalirAsync();
            _firebaseAuthService.CerrarSesion();
            await Navigation.PopToRootAsync();
        }

        private async void OnVerDatosClicked(object? sender, EventArgs e)
        {
            // Funcionalidad en desarrollo
            // TODO: Implementar visualización de datos
        }
    }
}
