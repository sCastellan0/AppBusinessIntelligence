using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class RecuperarContrasena : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;

        public RecuperarContrasena(FirebaseAuthService firebaseAuthService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
        }

        private async void OnEnviarClicked(object? sender, EventArgs e)
        {
            string correo = CorreoEntry.Text?.Trim() ?? "";

            var (exito, mensaje) = await _firebaseAuthService.EnviarResetPasswordAsync(correo);

            if (exito)
            {
                await DisplayAlertAsync("Correo enviado", mensaje, "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", mensaje, "OK");
            }
        }

        private async void OnVolverTapped(object? sender, TappedEventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
