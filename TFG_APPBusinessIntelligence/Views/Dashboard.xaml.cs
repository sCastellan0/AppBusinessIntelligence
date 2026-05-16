using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Dashboard : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly DatabaseService _databaseService;

        public Dashboard(FirebaseAuthService firebaseAuthService, DatabaseService databaseService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _databaseService = databaseService;
        }

        private async void OnConfiguracionTapped(object sender, EventArgs e)
        {
            // Funcionalidad en desarrollo
            // TODO: Agregar página de configuración
        }

        private async void OnSalirTapped(object sender, EventArgs e)
        {
            // Confirmar salida
            bool answer = await DisplayAlert("Cerrar sesión", "¿Deseas cerrar sesión?", "Sí", "No");

            if (answer)
            {
                // Cerrar sesión en Firebase
                _firebaseAuthService.CerrarSesion();

                // Volver al login
                await Navigation.PopToRootAsync();
            }
        }

        private async void OnVerDatosClicked(object sender, EventArgs e)
        {
            // Funcionalidad en desarrollo
            // TODO: Implementar visualización de datos
        }
    }
}
