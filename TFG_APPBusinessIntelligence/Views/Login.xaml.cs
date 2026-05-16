using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
	public partial class Login : ContentPage
	{
		public Login()
		{
			InitializeComponent();
		}
		private async void OnIniciarSesionClicked(object sender, EventArgs e)
		{
			// Obtener InicioSesion del contenedor de servicios
			var inicioSesion = App.Current!.Handler!.MauiContext!.Services.GetService<InicioSesion>();
			await Navigation.PushAsync(inicioSesion!);
		}

		private async void OnRegistroTapped(object sender, TappedEventArgs e)
		{
			// Obtener Registro del contenedor de servicios
			var registro = App.Current!.Handler!.MauiContext!.Services.GetService<Registro>();
			await Navigation.PushAsync(registro!);
		}

	}
}
