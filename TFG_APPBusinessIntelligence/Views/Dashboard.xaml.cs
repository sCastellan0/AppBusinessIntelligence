using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Dashboard : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly DatabaseService _databaseService;
        private readonly TotpService _totpService;
        private readonly ThemeService _themeService;
        private readonly IServiceProvider _serviceProvider;

        private readonly DatasetAnalyzerService _datasetAnalyzerService;
        private readonly GeneradorPdfService    _generadorPdfService;
        private readonly IFolderPickerService   _folderPickerService;
        private readonly IFileSaverService      _fileSaverService;

        private const string PrefCarpeta = "carpeta_informes";
        private string? _carpetaInformes;
        private bool _dialogoMostrado = false;

        public Dashboard(FirebaseAuthService firebaseAuthService, DatabaseService databaseService, TotpService totpService, ThemeService themeService, DatasetAnalyzerService datasetAnalyzerService, GeneradorPdfService generadorPdfService, IFolderPickerService folderPickerService, IFileSaverService fileSaverService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _databaseService = databaseService;
            _totpService = totpService;
            _themeService = themeService;
            _datasetAnalyzerService = datasetAnalyzerService;
            _generadorPdfService    = generadorPdfService;
            _folderPickerService    = folderPickerService;
            _fileSaverService       = fileSaverService;
            _serviceProvider        = serviceProvider;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _carpetaInformes = Preferences.Default.Get(PrefCarpeta, string.Empty);

            // Verificar si debe mostrarse el diálogo de configuración de carpeta
            if (!_dialogoMostrado && ConfigurarCarpetaDialog.DebesMostrarDialogo())
            {
                _dialogoMostrado = true;
                try
                {
                    await MostrarDialogoConfiguracionCarpeta();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error mostrando diálogo de carpeta: {ex.Message}");
                    // No propagar la excepción para evitar cierre inesperado
                }
            }
        }

        private async Task MostrarDialogoConfiguracionCarpeta()
        {
            try
            {
                var dialogo = _serviceProvider.GetRequiredService<ConfigurarCarpetaDialog>();
                await Navigation.PushModalAsync(dialogo, animated: true);
                var resultado = await dialogo.MostrarAsync();

                // Actualizar la carpeta si se configuró
                if (resultado)
                {
                    _carpetaInformes = Preferences.Default.Get(PrefCarpeta, string.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando diálogo de carpeta: {ex.Message}");
            }
        }

        private async void OnVerDatosClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_carpetaInformes))
            {
                var confirmDialog = ConfirmacionDialog.Crear(
                    "Carpeta No Configurada",
                    "Para ver los informes, primero necesitas configurar la carpeta donde se guardan.",
                    "Ir a Ajustes",
                    "Cancelar",
                    esPeligroso: false);

                await Navigation.PushModalAsync(confirmDialog, animated: true);
                bool irAjustes = await confirmDialog.MostrarAsync();

                if (irAjustes)
                {
                    var ajustes = new Ajustes(_firebaseAuthService, _databaseService, _totpService, _themeService, _folderPickerService);
                    await Navigation.PushAsync(ajustes);
                }
                return;
            }

            var pagina = new VerInformes(_fileSaverService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnCarpetaTapped(object? sender, TappedEventArgs e)
        {
            var pagina = new AnalizarDataset(_datasetAnalyzerService, _generadorPdfService, _fileSaverService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnConfiguracionTapped(object? sender, EventArgs e)
        {
            var ajustes = new Ajustes(_firebaseAuthService, _databaseService, _totpService, _themeService, _folderPickerService);
            await Navigation.PushAsync(ajustes);
        }

        private async void OnSalirTapped(object? sender, EventArgs e)
        {
            await MostrarPanelSalirAsync();
        }

        private async Task MostrarPanelSalirAsync()
        {
            PanelSalir.IsVisible = true;
            await PanelSalir.TranslateTo(0, 0, 280, Easing.CubicOut);
        }

        private async Task OcultarPanelSalirAsync()
        {
            await PanelSalir.TranslateTo(0, 300, 220, Easing.CubicIn);
            PanelSalir.IsVisible = false;
        }

        private async void OnCancelarSalirTapped(object? sender, TappedEventArgs e)
        {
            await OcultarPanelSalirAsync();
        }

        private async void OnConfirmarSalirTapped(object? sender, TappedEventArgs e)
        {
            await OcultarPanelSalirAsync();
            _firebaseAuthService.CerrarSesion();
            await Navigation.PopToRootAsync();
        }
    }
}

