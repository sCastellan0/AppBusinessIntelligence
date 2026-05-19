using Microsoft.Extensions.DependencyInjection;
using TFG_APPBusinessIntelligence.Services;
using TFG_APPBusinessIntelligence.Views;

namespace TFG_APPBusinessIntelligence
{
    public partial class App : Application
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly InactivityService _inactivityService;
        private readonly IServiceProvider _serviceProvider;

        public App(FirebaseAuthService firebaseAuthService, InactivityService inactivityService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _inactivityService = inactivityService;
            _serviceProvider = serviceProvider;

            // Aplicar tema guardado antes de crear la UI
            _serviceProvider.GetRequiredService<ThemeService>().AplicarTemaGuardado();

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

                    var popup = _serviceProvider.GetRequiredService<SesionExpiradaPopup>();
                    await Shell.Current.Navigation.PushModalAsync(popup, animated: true);
                    await popup.Resultado;

                    await Shell.Current.GoToAsync("//MainPage");
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
