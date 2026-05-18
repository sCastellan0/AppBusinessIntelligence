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
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretBytes);
        }

        /// <summary>
        /// Devuelve la URL otpauth:// para generar el código QR.
        /// </summary>
        public string GenerarUrlQR(string correo, string secret)
        {
            var issuerEncoded = Uri.EscapeDataString(Issuer);
            var correoEncoded = Uri.EscapeDataString(correo);
            return $"otpauth://totp/{issuerEncoded}:{correoEncoded}?secret={secret}&issuer={issuerEncoded}&algorithm=SHA1&digits=6&period=30";
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
                return totp.VerifyTotp(codigoUsuario.Trim(), out _, new VerificationWindow(1, 1));
            }
            catch
            {
                return false;
            }
        }
    }
}
