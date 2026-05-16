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

		private async void OnVolverClicked(object sender, EventArgs e)
		{
			// Navega atrás
			await Navigation.PopAsync();
		}

		private async void OnAccederClicked(object sender, EventArgs e)
		{
			// Obtiene los valores de los campos
			string usuario = UsuarioEntry.Text;
			string telefono = TelefonoEntry.Text;
			string correo = CorreoEntry.Text;
			string contrasena = ContrasenaEntry.Text;

			// Valida que no esten vacios
			if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(telefono) ||
				string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
			{
				await DisplayAlert("Error", "Por favor complete todos los campos", "OK");
				return;
			}

			// Intentar registrar usuario en Firebase
			var (exito, mensaje) = await _firebaseAuthService.RegistrarUsuarioAsync(
				usuario,
				correo,
				contrasena,
				telefono);

			if (exito)
			{
				await DisplayAlert("Éxito", "¡Cuenta creada exitosamente para " + usuario + "!", "OK");

				// Limpiar campos
				UsuarioEntry.Text = "";
				TelefonoEntry.Text = "";
				CorreoEntry.Text = "";
				ContrasenaEntry.Text = "";

				// Navegar de vuelta al Login
				await Navigation.PopAsync();
			}
			else
			{
				await DisplayAlert("Error", mensaje, "OK");
			}
		}
	}
}
