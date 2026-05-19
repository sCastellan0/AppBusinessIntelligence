using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class CambiarContrasena : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;

        public CambiarContrasena(FirebaseAuthService firebaseAuthService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
        }

        private async void OnGuardarClicked(object? sender, EventArgs e)
        {
            MostrarError(false, "");
            MostrarExito(false, "");

            string actual = ContrasenaActualEntry.Text ?? "";
            string nueva = ContrasenaNewEntry.Text ?? "";
            string confirmar = ContrasenaConfirmEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(nueva) || string.IsNullOrWhiteSpace(confirmar))
            {
                MostrarError(true, "Completa todos los campos");
                return;
            }

            if (nueva != confirmar)
            {
                MostrarError(true, "La nueva contrase\u00f1a y su confirmaci\u00f3n no coinciden");
                ContrasenaNewEntry.Text = "";
                ContrasenaConfirmEntry.Text = "";
                return;
            }

            if (nueva.Length < 6)
            {
                MostrarError(true, "La nueva contrase\u00f1a debe tener al menos 6 caracteres");
                return;
            }

            var (exito, mensaje) = await _firebaseAuthService.CambiarPasswordAsync(actual, nueva);

            if (exito)
            {
                MostrarExito(true, "\u2705 " + mensaje);
                await Task.Delay(1500);
                await Navigation.PopAsync();
            }
            else
            {
                MostrarError(true, mensaje);
                ContrasenaActualEntry.Text = "";
            }
        }

        private async void OnCancelarClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void MostrarError(bool visible, string mensaje)
        {
            ErrorBannerLabel.Text = mensaje;
            ErrorBanner.IsVisible = visible;
        }

        private void MostrarExito(bool visible, string mensaje)
        {
            ExitoBannerLabel.Text = mensaje;
            ExitoBanner.IsVisible = visible;
        }

        private void OnCerrarErrorTapped(object? sender, TappedEventArgs e)
        {
            MostrarError(false, "");
        }
    }
}
