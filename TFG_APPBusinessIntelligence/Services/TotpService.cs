using OtpNet;
using System.Text;

namespace TFG_APPBusinessIntelligence.Services
{
    public class TotpService
    {
        private const string Issuer = "AppBusinessIntelligence";

        /// <summary>
        /// Genera una nueva clave secreta TOTP en Base32.
        /// </summary>
        public string GenerarSecret()
        {
            // Generar exactamente 20 bytes (160 bits) de datos aleatorios
            // 20 bytes = 32 caracteres Base32 = tamaño estándar TOTP sin necesidad de padding
            var secretBytes = KeyGeneration.GenerateRandomKey(20);

            // Convertir a Base32 (el formato estándar para TOTP)
            var secret = Base32Encoding.ToString(secretBytes);

            System.Diagnostics.Debug.WriteLine($"[TotpService] Secret generado: {secret}");
            System.Diagnostics.Debug.WriteLine($"[TotpService] Longitud: {secret.Length} caracteres");
            System.Diagnostics.Debug.WriteLine($"[TotpService] Bytes (hex): {BitConverter.ToString(secretBytes)}");

            return secret;
        }

        /// <summary>
        /// Devuelve la URL otpauth:// para generar el código QR.
        /// </summary>
        public string GenerarUrlQR(string correo, string secret)
        {
            var issuerEncoded = Uri.EscapeDataString(Issuer);
            var correoEncoded = Uri.EscapeDataString(correo);

            // Microsoft Authenticator NO acepta padding '=' en la URL del QR
            // Quitarlo para el QR (Base32 sin padding es válido según RFC 4648)
            var secretSinPadding = secret.TrimEnd('=');

            // La URL debe seguir exactamente este formato para Microsoft Authenticator
            var url = $"otpauth://totp/{issuerEncoded}:{correoEncoded}?secret={secretSinPadding}&issuer={issuerEncoded}&algorithm=SHA1&digits=6&period=30";

            System.Diagnostics.Debug.WriteLine($"[TotpService] URL generada: {url}");
            System.Diagnostics.Debug.WriteLine($"[TotpService] Secret original (con padding): {secret}");
            System.Diagnostics.Debug.WriteLine($"[TotpService] Secret en URL (sin padding): {secretSinPadding}");
            System.Diagnostics.Debug.WriteLine($"[TotpService] Longitudes: original={secret.Length}, URL={secretSinPadding.Length}");

            return url;
        }

        /// <summary>
        /// Verifica si el código introducido por el usuario es válido.
        /// Admite una ventana de ±1 intervalo (30s) para compensar desincronía de reloj.
        /// </summary>
        public bool VerificarCodigo(string secret, string codigoUsuario)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(codigoUsuario))
                return false;

            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes);

                // Debug: imprimir códigos válidos en la ventana
                var ahora = DateTime.UtcNow;
                var codigoActual = totp.ComputeTotp(ahora);
                var codigoAnterior = totp.ComputeTotp(ahora.AddSeconds(-30));
                var codigoSiguiente = totp.ComputeTotp(ahora.AddSeconds(30));

                System.Diagnostics.Debug.WriteLine($"[TotpService] Hora UTC: {ahora:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[TotpService] Códigos válidos ahora:");
                System.Diagnostics.Debug.WriteLine($"  - Anterior (-30s): {codigoAnterior}");
                System.Diagnostics.Debug.WriteLine($"  - Actual: {codigoActual}");
                System.Diagnostics.Debug.WriteLine($"  - Siguiente (+30s): {codigoSiguiente}");
                System.Diagnostics.Debug.WriteLine($"[TotpService] Usuario ingresó: '{codigoUsuario.Trim()}'");

                var resultado = totp.VerifyTotp(codigoUsuario.Trim(), out _, new VerificationWindow(1, 1));
                System.Diagnostics.Debug.WriteLine($"[TotpService] Resultado verificación: {resultado}");

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TotpService] Error en verificación: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TotpService] Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
