using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int RequestCodePermisos = 100;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Hacer la barra de estado transparente
            if (Window != null)
            {
                Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    Window.SetDecorFitsSystemWindows(false);
                    var windowInsetsController = Window.InsetsController;
                    if (windowInsetsController != null)
                    {
                        windowInsetsController.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
                    }
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var flags = (int)Window.DecorView.SystemUiVisibility;
                    flags &= ~(int)SystemUiFlags.LightStatusBar;
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)flags;
                }
            }

            SolicitarPermisos();
        }

        private void SolicitarPermisos()
        {
            var permisosPendientes = new System.Collections.Generic.List<string>();

            // Notificaciones (Android 13+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.PostNotifications) != Permission.Granted)
                    permisosPendientes.Add(Android.Manifest.Permission.PostNotifications);
            }

            // Almacenamiento de archivos (Android 6-10)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadExternalStorage) != Permission.Granted)
                    permisosPendientes.Add(Android.Manifest.Permission.ReadExternalStorage);

                if (Build.VERSION.SdkInt <= BuildVersionCodes.P &&
                    ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
                    permisosPendientes.Add(Android.Manifest.Permission.WriteExternalStorage);
            }
            // Android 11+ usa el selector de archivos del sistema (SAF), no necesita permiso runtime

            if (permisosPendientes.Count > 0)
                ActivityCompat.RequestPermissions(this, permisosPendientes.ToArray(), RequestCodePermisos);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            // Resultado gestionado por MAUI Permissions internamente
        }

        public override bool DispatchTouchEvent(MotionEvent? ev)
        {
            var inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();
            inactivityService?.ReiniciarTemporizador();
            return base.DispatchTouchEvent(ev);
        }
    }
}
