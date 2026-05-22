using TFG_APPBusinessIntelligence.Models;
using TFG_APPBusinessIntelligence.Services;

namespace TFG_APPBusinessIntelligence.Views
{
    public partial class Configuracion2FA : ContentPage
    {
        private readonly TotpService _totpService;
        private readonly DatabaseService _databaseService;
        private readonly FirebaseAuthService _firebaseAuthService;

        private string? _secretTemporal;

        public Configuracion2FA(TotpService totpService, DatabaseService databaseService, FirebaseAuthService firebaseAuthService)
        {
            InitializeComponent();
            _totpService = totpService;
            _databaseService = databaseService;
            _firebaseAuthService = firebaseAuthService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarEstadoAsync();
        }

        private async Task CargarEstadoAsync()
        {
            var correo = _firebaseAuthService.ObtenerCorreoUsuario();
            if (string.IsNullOrEmpty(correo))
            {
                EstadoLabel.Text = "⚠️ No hay sesión activa";
                return;
            }

            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);

            // Si el usuario existe en Firebase pero no tiene fila en SQLite, crearla
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    NombreUsuario = correo,
                    Correo = correo,
                    PasswordHash = string.Empty,
                    FechaRegistro = DateTime.Now
                };
                await _databaseService.SaveUsuarioAsync(usuario);
                // Recargar para obtener el Id asignado
                usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);
                if (usuario == null) return;
            }

            if (usuario.Tiene2FA)
            {
                EstadoLabel.Text = "✅ El 2FA está ACTIVADO en tu cuenta";
                EstadoFrame.BackgroundColor = Color.FromArgb("#14532D");
                EstadoFrame.BorderColor = Color.FromArgb("#22C55E");
                PanelActivacion.IsVisible = false;
                PanelDesactivacion.IsVisible = true;
            }
            else
            {
                EstadoLabel.Text = "⚠️ El 2FA está DESACTIVADO. Actívalo para mayor seguridad.";
                EstadoFrame.BackgroundColor = Color.FromArgb("#7C2D12");
                EstadoFrame.BorderColor = Color.FromArgb("#EF4444");

                // Solo generar un nuevo secret si no existe uno temporal
                if (string.IsNullOrEmpty(_secretTemporal))
                {
                    _secretTemporal = _totpService.GenerarSecret();
                }

                SecretLabel.Text = _secretTemporal;

                // Agregar debug info
                System.Diagnostics.Debug.WriteLine($"[2FA] Secret generado: {_secretTemporal}");

                PanelActivacion.IsVisible = true;
                PanelDesactivacion.IsVisible = false;
            }
        }

        private async void OnActivarClicked(object? sender, EventArgs e)
        {
            string codigo = CodigoConfirmacionEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "Introduce el código de 6 dígitos de tu app");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                return;
            }

            if (string.IsNullOrEmpty(_secretTemporal))
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "No se generó el secret. Recarga la pantalla.");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[2FA] Verificando código: {codigo}");
            System.Diagnostics.Debug.WriteLine($"[2FA] Secret usado: {_secretTemporal}");
            System.Diagnostics.Debug.WriteLine($"[2FA] Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

            // Generar código actual para debug
            try
            {
                var secretBytes = OtpNet.Base32Encoding.ToBytes(_secretTemporal);
                var totp = new OtpNet.Totp(secretBytes);
                var codigoEsperado = totp.ComputeTotp(DateTime.UtcNow);
                System.Diagnostics.Debug.WriteLine($"[2FA] Código esperado en este momento: {codigoEsperado}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2FA] Error generando código esperado: {ex.Message}");
            }

            bool valido = _totpService.VerificarCodigo(_secretTemporal, codigo);
            System.Diagnostics.Debug.WriteLine($"[2FA] Resultado verificación: {valido}");

            if (!valido)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Código Incorrecto",
                    "El código no coincide.",
                    "Asegúrate de haber escaneado el QR correctamente y que el reloj de tu dispositivo esté sincronizado.");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                CodigoConfirmacionEntry.Text = "";
                return;
            }

            var correo = _firebaseAuthService.ObtenerCorreoUsuario()!;
            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);

            if (usuario == null)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "No se encontró el usuario en la base de datos local");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                return;
            }

            usuario.TotpSecret = _secretTemporal;
            usuario.Tiene2FA = true;
            await _databaseService.SaveUsuarioAsync(usuario);

            var exitoDialog = NotificacionDialog.Exito(
                "¡2FA Activado!",
                "La verificación en dos pasos está ahora activa.",
                "Tu cuenta está protegida con autenticación de dos factores.");
            await Navigation.PushModalAsync(exitoDialog, animated: true);
            await exitoDialog.MostrarAsync();

            await CargarEstadoAsync();
        }

        private async void OnDesactivarClicked(object? sender, EventArgs e)
        {
            string codigo = CodigoDesactivarEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "Introduce el código de 6 dígitos para confirmar");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                return;
            }

            var correo = _firebaseAuthService.ObtenerCorreoUsuario()!;
            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);

            if (usuario == null || string.IsNullOrEmpty(usuario.TotpSecret))
            {
                var errorDialog = NotificacionDialog.Error(
                    "Error",
                    "No se encontró la configuración 2FA");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                return;
            }

            bool valido = _totpService.VerificarCodigo(usuario.TotpSecret, codigo);

            if (!valido)
            {
                var errorDialog = NotificacionDialog.Error(
                    "Código Incorrecto",
                    "El código no es válido.");
                await Navigation.PushModalAsync(errorDialog, animated: true);
                await errorDialog.MostrarAsync();
                CodigoDesactivarEntry.Text = "";
                return;
            }

            var confirmarDialog = ConfirmacionDialog.Crear(
                "Desactivar 2FA",
                "¿Seguro que quieres desactivar la verificación en dos pasos? Tu cuenta será menos segura.",
                "Sí, desactivar",
                "Cancelar",
                esPeligroso: true);

            await Navigation.PushModalAsync(confirmarDialog, animated: true);
            bool confirmar = await confirmarDialog.MostrarAsync();

            if (!confirmar) return;

            usuario.TotpSecret = null;
            usuario.Tiene2FA = false;
            await _databaseService.SaveUsuarioAsync(usuario);

            var exitoDialog = NotificacionDialog.Advertencia(
                "2FA Desactivado",
                "La verificación en dos pasos ha sido desactivada.",
                "Puedes volver a activarla en cualquier momento desde esta pantalla.");
            await Navigation.PushModalAsync(exitoDialog, animated: true);
            await exitoDialog.MostrarAsync();

            await CargarEstadoAsync();
        }

        private async void OnVolverClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnCopiarSecretClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_secretTemporal))
            {
                return;
            }

            try
            {
                await Clipboard.Default.SetTextAsync(_secretTemporal);

                var dialog = NotificacionDialog.Exito(
                    "Copiado",
                    "El secret se ha copiado al portapapeles.",
                    $"Puedes introducirlo manualmente en Microsoft Authenticator:\n\n{_secretTemporal}");
                await Navigation.PushModalAsync(dialog, animated: true);
                await dialog.MostrarAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[2FA] Error al copiar: {ex.Message}");
            }
        }
    }
}
