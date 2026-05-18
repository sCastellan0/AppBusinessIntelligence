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
            string actual = ContrasenaActualEntry.Text ?? "";
            string nueva = ContrasenaNewEntry.Text ?? "";
            string confirmar = ContrasenaConfirmEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(nueva) || string.IsNullOrWhiteSpace(confirmar))
            {
                await DisplayAlertAsync("Error", "Completa todos los campos", "OK");
                return;
            }

            if (nueva != confirmar)
            {
                await DisplayAlertAsync("Error", "La nueva contraseña y su confirmación no coinciden", "OK");
                ContrasenaNewEntry.Text = "";
                ContrasenaConfirmEntry.Text = "";
                return;
            }

            if (nueva.Length < 6)
            {
                await DisplayAlertAsync("Error", "La nueva contraseña debe tener al menos 6 caracteres", "OK");
                return;
            }

            var (exito, mensaje) = await _firebaseAuthService.CambiarPasswordAsync(actual, nueva);

            if (exito)
            {
                await DisplayAlertAsync("✅ Contraseña actualizada", mensaje, "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", mensaje, "OK");
                ContrasenaActualEntry.Text = "";
            }
        }

        private async void OnCancelarClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
