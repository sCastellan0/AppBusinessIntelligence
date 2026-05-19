using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
	public partial class Registro : ContentPage
	{
		private readonly FirebaseAuthService _firebaseAuthService;

		public Registro(FirebaseAuthService firebaseAuthService)
		{
			InitializeComponent();
			_firebaseAuthService = firebaseAuthService;
		}

		private async void OnVolverClicked(object? sender, EventArgs e)
		{
			// Navega atrás
			await Navigation.PopAsync();
		}

		private async void OnAccederClicked(object? sender, EventArgs e)
		{
			MostrarError(false, "");

			string usuario = UsuarioEntry.Text;
			string telefono = TelefonoEntry.Text;
			string correo = CorreoEntry.Text;
			string contrasena = ContrasenaEntry.Text;

			if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(telefono) ||
				string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
			{
				MostrarError(true, "Por favor complete todos los campos");
				return;
			}

			var (exito, mensaje) = await _firebaseAuthService.RegistrarUsuarioAsync(
				usuario,
				correo,
				contrasena,
				telefono);

			if (exito)
			{
				UsuarioEntry.Text = "";
				TelefonoEntry.Text = "";
				CorreoEntry.Text = "";
				ContrasenaEntry.Text = "";

				await Navigation.PopAsync();
			}
			else
			{
				MostrarError(true, mensaje);
			}
		}

		private void MostrarError(bool visible, string mensaje)
		{
			ErrorBannerLabel.Text = mensaje;
			ErrorBanner.IsVisible = visible;
		}

		private void OnCerrarErrorTapped(object? sender, TappedEventArgs e)
		{
			MostrarError(false, "");
		}
	}
}
