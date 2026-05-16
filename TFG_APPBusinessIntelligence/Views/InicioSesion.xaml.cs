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

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            // Navega atrás
            await Navigation.PopAsync();
        }

        private async void OnAccederClicked(object sender, EventArgs e)
        {
            // Obtiene los valores de los campos
            string correo = UsuarioEntry.Text;
            string contrasena = ContrasenaEntry.Text;

            // Valida que no esten vacios
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
            {
                await DisplayAlert("Error", "Por favor ingrese correo y contraseña", "OK");
                return;
            }

            // Intentar iniciar sesión con Firebase
            var (exito, mensaje) = await _firebaseAuthService.IniciarSesionAsync(correo, contrasena);

            if (exito)
            {
                // Limpiar campos
                UsuarioEntry.Text = "";
                ContrasenaEntry.Text = "";

                // Navegar al Dashboard
                var dashboard = App.Current!.Handler!.MauiContext!.Services.GetService<Dashboard>();
                await Navigation.PushAsync(dashboard!);
            }
            else
            {
                await DisplayAlert("Error", mensaje, "OK");
            }
        }

    }
}
