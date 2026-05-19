using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Ajustes : ContentPage
    {
        private readonly FirebaseAuthService _firebaseAuthService;
        private readonly DatabaseService _databaseService;
        private readonly TotpService _totpService;
        private readonly ThemeService _themeService;
        private bool _cargandoNotif = false;
        private bool _cargandoAlmacen = false;

        private const string ClaveNotifActivas = "notificaciones_activas";

        public Ajustes(FirebaseAuthService firebaseAuthService, DatabaseService databaseService, TotpService totpService, ThemeService themeService)
        {
            InitializeComponent();
            _firebaseAuthService = firebaseAuthService;
            _databaseService = databaseService;
            _totpService = totpService;
            _themeService = themeService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ActualizarEstado2FAAsync();
            await SincronizarEstadoNotificacionesAsync();
            await SincronizarEstadoAlmacenamientoAsync();
            ActualizarBotonesTema();
        }

        private void ActualizarBotonesTema()
        {
            bool esOscuro = _themeService.TemaActual == AppTema.Oscuro;
            EstadoTemaLabel.Text = esOscuro ? "🌙 Tema Oscuro activo" : "☀️ Tema Azul Claro activo";

            BtnTemaOscuro.BackgroundColor = esOscuro ? Color.FromArgb("#3B82F6") : Color.FromArgb("#334155");
            BtnTemaOscuro.TextColor = Color.FromArgb("#FFFFFF");

            BtnTemaAzulClaro.BackgroundColor = esOscuro ? Color.FromArgb("#334155") : Color.FromArgb("#3B82F6");
            BtnTemaAzulClaro.TextColor = Color.FromArgb("#FFFFFF");
        }

        private void OnTemaOscuroClicked(object sender, EventArgs e)
        {
            _themeService.AplicarTema(AppTema.Oscuro);
            ActualizarBotonesTema();
        }

        private void OnTemaAzulClaroClicked(object sender, EventArgs e)
        {
            _themeService.AplicarTema(AppTema.AzulClaro);
            ActualizarBotonesTema();
        }

        private async Task SincronizarEstadoNotificacionesAsync()
        {
#if ANDROID
            var estado = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            bool activas = estado == PermissionStatus.Granted;
            Preferences.Set(ClaveNotifActivas, activas);
            _cargandoNotif = true;
            NotifSwitch.IsToggled = activas;
            _cargandoNotif = false;
            ActualizarEtiquetaNotif(activas);
#else
            CargarEstadoNotificaciones();
#endif
        }

        private async Task ActualizarEstado2FAAsync()
        {
            var correo = _firebaseAuthService.ObtenerCorreoUsuario();
            if (string.IsNullOrEmpty(correo)) return;

            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);
            Estado2FALabel.Text = (usuario?.Tiene2FA == true)
                ? "✅ Activado"
                : "⚠️ Desactivado — toca para configurar";
        }

        private void CargarEstadoNotificaciones()
        {
            _cargandoNotif = true;
            bool activas = Preferences.Get(ClaveNotifActivas, true);
            NotifSwitch.IsToggled = activas;
            ActualizarEtiquetaNotif(activas);
            _cargandoNotif = false;
        }

        private void ActualizarEtiquetaNotif(bool activas)
        {
            EstadoNotifLabel.Text = activas ? "✅ Activadas" : "⛔ Desactivadas";
        }

        private async void OnNotifToggled(object sender, ToggledEventArgs e)
        {
            if (_cargandoNotif) return;

            bool activar = e.Value;

#if ANDROID
            if (activar)
            {
                // Comprobar estado actual antes de pedir
                var estadoActual = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();

                if (estadoActual == PermissionStatus.Granted)
                {
                    Preferences.Set(ClaveNotifActivas, true);
                    ActualizarEtiquetaNotif(true);
                    return;
                }

                // Denied permanentemente → hay que ir a ajustes del sistema
                if (estadoActual == PermissionStatus.Denied || estadoActual == PermissionStatus.Restricted)
                {
                    _cargandoNotif = true;
                    NotifSwitch.IsToggled = false;
                    _cargandoNotif = false;
                    ActualizarEtiquetaNotif(false);
                    Preferences.Set(ClaveNotifActivas, false);

                    bool irAjustes = await DisplayAlert(
                        "Permiso necesario",
                        "El permiso de notificaciones fue denegado. Debes activarlo manualmente desde los ajustes del sistema.",
                        "Abrir ajustes", "Cancelar");

                    if (irAjustes)
                        AbrirAjustesSistema();
                    return;
                }

                // Primera vez: solicitar normalmente
                var resultado = await Permissions.RequestAsync<Permissions.PostNotifications>();
                if (resultado != PermissionStatus.Granted)
                {
                    _cargandoNotif = true;
                    NotifSwitch.IsToggled = false;
                    _cargandoNotif = false;
                    ActualizarEtiquetaNotif(false);
                    Preferences.Set(ClaveNotifActivas, false);
                    return;
                }

                Preferences.Set(ClaveNotifActivas, true);
                ActualizarEtiquetaNotif(true);
            }
            else
            {
                var popup = new NotificacionesPopup();
                await Navigation.PushModalAsync(popup);
                bool irAjustes = await popup.Resultado;

                _cargandoNotif = true;
                NotifSwitch.IsToggled = true;
                _cargandoNotif = false;

                if (irAjustes)
                    AbrirAjustesSistema();
            }
#endif

            Preferences.Set(ClaveNotifActivas, activar);
            ActualizarEtiquetaNotif(activar);
        }

        private async Task SincronizarEstadoAlmacenamientoAsync()
        {
#if ANDROID
            bool activo;
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
            {
                // Android 11+: FilePicker/SAF no necesita permiso runtime
                activo = true;
            }
            else
            {
                var estado = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                activo = estado == PermissionStatus.Granted;
            }

            _cargandoAlmacen = true;
            AlmacenSwitch.IsToggled = activo;
            _cargandoAlmacen = false;
            ActualizarEtiquetaAlmacen(activo);
#else
            ActualizarEtiquetaAlmacen(true);
#endif
        }

        private void ActualizarEtiquetaAlmacen(bool activo)
        {
            EstadoAlmacenLabel.Text = activo ? "✅ Acceso a archivos permitido" : "⛔ Sin acceso a archivos";
        }

        private async void OnAlmacenToggled(object sender, ToggledEventArgs e)
        {
            if (_cargandoAlmacen) return;

            bool activar = e.Value;

#if ANDROID
            if (activar)
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
                    // Android 11+: FilePicker/SAF no necesita permiso runtime, siempre disponible
                    ActualizarEtiquetaAlmacen(true);
                    return;
                }

                var estadoActual = await Permissions.CheckStatusAsync<Permissions.StorageRead>();

                if (estadoActual == PermissionStatus.Granted)
                {
                    ActualizarEtiquetaAlmacen(true);
                    return;
                }

                if (estadoActual == PermissionStatus.Denied || estadoActual == PermissionStatus.Restricted)
                {
                    _cargandoAlmacen = true;
                    AlmacenSwitch.IsToggled = false;
                    _cargandoAlmacen = false;
                    ActualizarEtiquetaAlmacen(false);

                    bool irAjustes = await DisplayAlert(
                        "Permiso necesario",
                        "El permiso de almacenamiento fue denegado. Debes activarlo manualmente desde los ajustes del sistema.",
                        "Abrir ajustes", "Cancelar");

                    if (irAjustes)
                        AbrirAjustesSistema();
                    return;
                }

                var resultado = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (resultado != PermissionStatus.Granted)
                {
                    _cargandoAlmacen = true;
                    AlmacenSwitch.IsToggled = false;
                    _cargandoAlmacen = false;
                    ActualizarEtiquetaAlmacen(false);
                    return;
                }
                ActualizarEtiquetaAlmacen(true);
            }
            else
            {
                var popup = new NotificacionesPopup();
                await Navigation.PushModalAsync(popup);
                bool irAjustes = await popup.Resultado;

                _cargandoAlmacen = true;
                AlmacenSwitch.IsToggled = true;
                _cargandoAlmacen = false;

                if (irAjustes)
                    AbrirAjustesSistema();
            }
#endif
        }

        private void AbrirAjustesSistema()
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.SetData(Android.Net.Uri.Parse("package:" + context.PackageName));
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
#endif
        }

        private async void OnConfigurar2FATapped(object sender, EventArgs e)
        {
            var pagina = new Configuracion2FA(_totpService, _databaseService, _firebaseAuthService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnCambiarContrasenaTapped(object sender, EventArgs e)
        {
            var pagina = new CambiarContrasena(_firebaseAuthService);
            await Navigation.PushAsync(pagina);
        }

        private async void OnVolverTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
