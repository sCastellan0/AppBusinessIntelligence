using QRCoder;
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

                _secretTemporal = _totpService.GenerarSecret();
                string urlQR = _totpService.GenerarUrlQR(correo, _secretTemporal);

                CodigoQR.Source = GenerarImagenQR(urlQR);
                SecretLabel.Text = _secretTemporal;
                PanelActivacion.IsVisible = true;
                PanelDesactivacion.IsVisible = false;
            }
        }

        private static ImageSource GenerarImagenQR(string contenido)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
            var pngQr = new PngByteQRCode(qrData);
            byte[] pngBytes = pngQr.GetGraphic(10);

            // Guardar en archivo temporal — FromFile es el método más fiable en Android MAUI
            string rutaTemporal = Path.Combine(FileSystem.CacheDirectory, "totp_qr.png");
            File.WriteAllBytes(rutaTemporal, pngBytes);
            return ImageSource.FromFile(rutaTemporal);
        }

        private async void OnActivarClicked(object? sender, EventArgs e)
        {
            string codigo = CodigoConfirmacionEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                await DisplayAlertAsync("Error", "Introduce el código de 6 dígitos de tu app", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_secretTemporal))
            {
                await DisplayAlertAsync("Error", "No se generó el secret. Recarga la pantalla.", "OK");
                return;
            }

            bool valido = _totpService.VerificarCodigo(_secretTemporal, codigo);

            if (!valido)
            {
                await DisplayAlertAsync("Código incorrecto", "El código no coincide. Asegúrate de haber escaneado el QR correctamente.", "OK");
                CodigoConfirmacionEntry.Text = "";
                return;
            }

            var correo = _firebaseAuthService.ObtenerCorreoUsuario()!;
            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);

            if (usuario == null)
            {
                await DisplayAlertAsync("Error", "No se encontró el usuario en la base de datos local", "OK");
                return;
            }

            usuario.TotpSecret = _secretTemporal;
            usuario.Tiene2FA = true;
            await _databaseService.SaveUsuarioAsync(usuario);

            await DisplayAlertAsync("¡Activado!", "La verificación en dos pasos está ahora activa en tu cuenta.", "OK");
            await CargarEstadoAsync();
        }

        private async void OnDesactivarClicked(object? sender, EventArgs e)
        {
            string codigo = CodigoDesactivarEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                await DisplayAlertAsync("Error", "Introduce el código de 6 dígitos para confirmar", "OK");
                return;
            }

            var correo = _firebaseAuthService.ObtenerCorreoUsuario()!;
            var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);

            if (usuario == null || string.IsNullOrEmpty(usuario.TotpSecret))
            {
                await DisplayAlertAsync("Error", "No se encontró la configuración 2FA", "OK");
                return;
            }

            bool valido = _totpService.VerificarCodigo(usuario.TotpSecret, codigo);

            if (!valido)
            {
                await DisplayAlertAsync("Código incorrecto", "El código no es válido.", "OK");
                CodigoDesactivarEntry.Text = "";
                return;
            }

            bool confirmar = await DisplayAlertAsync("Confirmar", "¿Seguro que quieres desactivar el 2FA?", "Sí, desactivar", "Cancelar");
            if (!confirmar) return;

            usuario.TotpSecret = null;
            usuario.Tiene2FA = false;
            await _databaseService.SaveUsuarioAsync(usuario);

            await DisplayAlertAsync("Desactivado", "La verificación en dos pasos ha sido desactivada.", "OK");
            await CargarEstadoAsync();
        }

        private async void OnVolverClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
