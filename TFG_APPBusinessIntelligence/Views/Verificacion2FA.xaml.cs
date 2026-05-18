using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Verificacion2FA : ContentPage
    {
        private readonly TotpService _totpService;
        private readonly DatabaseService _databaseService;
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly string _correo;

        public Verificacion2FA(TotpService totpService, DatabaseService databaseService, FirebaseAuthService firebaseAuthService, string correo)
        {
            InitializeComponent();
            _totpService = totpService;
            _databaseService = databaseService;
            _firebaseAuthService = firebaseAuthService;
            _correo = correo;
        }

        private async void OnVerificarClicked(object? sender, EventArgs e)
        {
            string codigo = CodigoEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                await DisplayAlertAsync("Error", "Introduce un código de 6 dígitos", "OK");
                return;
            }

            var usuario = await _databaseService.GetUsuarioByCorreoAsync(_correo);

            if (usuario == null || string.IsNullOrEmpty(usuario.TotpSecret))
            {
                await DisplayAlertAsync("Error", "No se encontró la configuración 2FA del usuario", "OK");
                return;
            }

            bool valido = _totpService.VerificarCodigo(usuario.TotpSecret, codigo);

            if (valido)
            {
                CodigoEntry.Text = "";
                var dashboard = App.Current!.Handler!.MauiContext!.Services.GetService<Dashboard>();
                await Navigation.PushAsync(dashboard!);
            }
            else
            {
                await DisplayAlertAsync("Código incorrecto", "El código introducido no es válido. Comprueba tu app autenticadora.", "OK");
                CodigoEntry.Text = "";
            }
        }

        private async void OnCancelarClicked(object? sender, EventArgs e)
        {
            _firebaseAuthService.CerrarSesion();
            await Navigation.PopToRootAsync();
        }
    }
}
