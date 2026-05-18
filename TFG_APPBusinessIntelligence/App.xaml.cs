using Microsoft.Extensions.DependencyInjection;
using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence
{
    public partial class App : Application
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly InactivityService _inactivityService;

        public App(FirebaseAuthService firebaseAuthService, InactivityService inactivityService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _inactivityService = inactivityService;

            _inactivityService.SesionExpirada += OnSesionExpirada;
        }

        private void OnSesionExpirada(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_firebaseAuthService.EstaAutenticado)
                {
                    _inactivityService.Detener();
                    _firebaseAuthService.CerrarSesion();

                    if (MainPage is NavigationPage navPage)
                    {
                        await navPage.Navigation.PopToRootAsync();
                    }

                    await Shell.Current.DisplayAlert(
                        "Sesión cerrada",
                        "Tu sesión ha expirado por inactividad.",
                        "Aceptar");
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
