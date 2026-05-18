using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Ajustes : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly DatabaseService _databaseService;
        private readonly TotpService _totpService;

        public Ajustes(FirebaseAuthService firebaseAuthService, DatabaseService databaseService, TotpService totpService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _databaseService = databaseService;
            _totpService = totpService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ActualizarEstado2FAAsync();
        }

        private async Task ActualizarEstado2FAAsync()
        {
            var correo = _firebaseAuthService.ObtenerCorreoUsuario();
            if (string.IsNullOrEmpty(correo)) return;

            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);
            Estado2FALabel.Text = (usuario?.Tiene2FA == true)
                ? "✅ Activado"
                : "⚠️ Desactivado — toca para configurar";
        }

        private async void OnConfigurar2FATapped(object sender, EventArgs e)
        {
            var pagina = new Configuracion2FA(_totpService, _databaseService, _firebaseAuthService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnCambiarContrasenaTapped(object sender, EventArgs e)
        {
            var pagina = new CambiarContrasena(_firebaseAuthService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnVolverTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
