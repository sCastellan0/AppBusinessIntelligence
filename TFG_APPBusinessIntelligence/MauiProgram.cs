using Microsoft.Extensions.Logging;
using TFG_APPBusinessIntelligence.Services;
using TFG_APPBusinessIntelligence.Views;

namespace TFG_APPBusinessIntelligence
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registrar servicios
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<FirebaseAuthService>();

            // Registrar páginas
            builder.Services.AddTransient<InicioSesion>();
            builder.Services.AddTransient<Registro>();
            builder.Services.AddTransient<Dashboard>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
