using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Hacer la barra de estado transparente
            if (Window != null)
            {
                Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

                // Configurar el color de los iconos de la barra de estado (claro/oscuro)
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    Window.SetDecorFitsSystemWindows(false);
                    var windowInsetsController = Window.InsetsController;
                    if (windowInsetsController != null)
                    {
                        // Iconos claros para fondo oscuro
                        windowInsetsController.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
                    }
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var flags = (int)Window.DecorView.SystemUiVisibility;
                    // Quitar la bandera de iconos oscuros para usar iconos claros
                    flags &= ~(int)SystemUiFlags.LightStatusBar;
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)flags;
                }
            }
        }

        public override bool DispatchTouchEvent(MotionEvent? ev)
        {
            var inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();
            inactivityService?.ReiniciarTemporizador();
            return base.DispatchTouchEvent(ev);
        }
    }
}
