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
                await MostrarBannerExitoAsync();
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlertAsync("Error", mensaje, "OK");
            }
        }

        private async Task MostrarBannerExitoAsync()
        {
            BannerExito.IsVisible = true;
            await BannerExito.FadeTo(1, 300, Easing.CubicOut);
            await Task.Delay(2200);
            await BannerExito.FadeTo(0, 400, Easing.CubicIn);
            BannerExito.IsVisible = false;
        }

        private async void OnVolverTapped(object? sender, TappedEventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
