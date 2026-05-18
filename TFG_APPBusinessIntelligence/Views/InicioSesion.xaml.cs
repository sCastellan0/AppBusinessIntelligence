using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class InicioSesion : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;

        public InicioSesion(FirebaseAuthService firebaseAuthService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
        }

        private async void OnVolverClicked(object? sender, EventArgs e)
        {
            // Navega atrás
            await Navigation.PopAsync();
        }

        private async void OnRecuperarContrasenaTapped(object? sender, TappedEventArgs e)
        {
            var firebaseAuthService = App.Current!.Handler!.MauiContext!.Services.GetService<FirebaseAuthService>();
            var pagina = new RecuperarContrasena(firebaseAuthService!);
            await Navigation.PushAsync(pagina);
        }

        private void MostrarError(string mensaje)
        {
            ErrorBannerLabel.Text = mensaje;
            ErrorBanner.IsVisible = true;
        }

        private void OnCerrarErrorTapped(object? sender, TappedEventArgs e)
        {
            ErrorBanner.IsVisible = false;
        }

        private async void OnAccederClicked(object? sender, EventArgs e)
        {
            ErrorBanner.IsVisible = false;

            string correo = UsuarioEntry.Text;
            string contrasena = ContrasenaEntry.Text;

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                MostrarError("Por favor ingrese correo y contraseña");
                return;
            }

            var (exito, mensaje, requiere2FA) = await _firebaseAuthService.IniciarSesionAsync(correo, contrasena);

            if (exito)
            {
                UsuarioEntry.Text = "";
                ContrasenaEntry.Text = "";

                if (requiere2FA)
                {
                    var totpService = App.Current!.Handler!.MauiContext!.Services.GetService<TotpService>();
                    var databaseService = App.Current!.Handler!.MauiContext!.Services.GetService<DatabaseService>();
                    var verificacion = new Verificacion2FA(totpService!, databaseService!, _firebaseAuthService, correo);
                    await Navigation.PushAsync(verificacion);
                }
                else
                {
                    var dashboard = App.Current!.Handler!.MauiContext!.Services.GetService<Dashboard>();
                    await Navigation.PushAsync(dashboard!);
                }
            }
            else
            {
                MostrarError(mensaje);
            }
        }
    }
}
