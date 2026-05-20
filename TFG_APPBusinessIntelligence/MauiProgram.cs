using Microsoft.Extensions.Logging;
using TFG_APPBusinessIntelligence.Services;
using TFG_APPBusinessIntelligence.Views;
using ZXing.Net.Maui.Controls;
#if ANDROID
using TFG_APPBusinessIntelligence.Platforms.Android;
#endif

namespace TFG_APPBusinessIntelligence
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler<SesionExpiradaPopup, TransparentModalPageHandler>();
#endif
                });

            // Registrar servicios
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<FirebaseAuthService>();
            builder.Services.AddSingleton<TotpService>();
            builder.Services.AddSingleton<InactivityService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<DatasetAnalyzerService>();
            builder.Services.AddSingleton<GeneradorPdfService>();
#if ANDROID || WINDOWS
            builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
            builder.Services.AddSingleton<IFileSaverService, FileSaverService>();
#endif

            // Registrar páginas
            builder.Services.AddTransient<InicioSesion>();
            builder.Services.AddTransient<Registro>();
            builder.Services.AddTransient<Dashboard>();
            builder.Services.AddTransient<Ajustes>();
            builder.Services.AddTransient<RecuperarContrasena>();
            builder.Services.AddTransient<CambiarContrasena>();
            builder.Services.AddTransient<SesionExpiradaPopup>();
            builder.Services.AddTransient<AnalizarDataset>();
            builder.Services.AddTransient<VerInformes>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
