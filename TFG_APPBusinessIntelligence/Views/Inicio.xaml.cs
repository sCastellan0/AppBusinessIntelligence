namespace TFG_APPBusinessIntelligence.Views
{
	public partial class Inicio : ContentPage
	{
		public Inicio()
		{
			InitializeComponent();
		}
		private async void OnLogoTapped(object sender, TappedEventArgs e)
		{
			// Navegar a la página de Login
			await Navigation.PushAsync(new Login());
		}
	}
}