using Firebase.Auth;
using Firebase.Auth.Providers;
using TFG_APPBusinessIntelligence.Models;

namespace TFG_APPBusinessIntelligence.Services
{
    public class FirebaseAuthService
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly DatabaseService _databaseService;
        private UserCredential? _usuarioActual;

        public UserCredential? UsuarioActual => _usuarioActual;
        public bool EstaAutenticado => _usuarioActual != null;

        public FirebaseAuthService(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);
        }

        public async Task<(bool exito, string mensaje)> RegistrarUsuarioAsync(
            string nombreUsuario,
            string correo,
            string password,
            string? telefono = null)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(nombreUsuario))
                    return (false, "El nombre de usuario es requerido");

                if (string.IsNullOrWhiteSpace(correo))
                    return (false, "El correo es requerido");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "La contraseña es requerida");

                if (password.Length < 6)
                    return (false, "La contraseña debe tener al menos 6 caracteres");

                // Verificar si el usuario ya existe en SQLite local
                if (await _databaseService.ExisteUsuarioAsync(nombreUsuario))
                    return (false, "El nombre de usuario ya está en uso");

                // Registrar en Firebase Authentication
                var credential = await _authClient.CreateUserWithEmailAndPasswordAsync(correo, password);

                if (credential?.User == null)
                    return (false, "Error al crear usuario en Firebase");

                // Guardar también en SQLite local
                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = nombreUsuario,
                    PasswordHash = password, // En producción, hashear
                    Correo = correo,
                    Telefono = telefono,
                    FechaRegistro = DateTime.Now
                };

                await _databaseService.SaveUsuarioAsync(nuevoUsuario);

                return (true, "Usuario registrado exitosamente");
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ObtenerMensajeError(ex));
            }
            catch (Exception ex)
            {
                return (false, $"Error al registrar usuario: {ex.Message}");
            }
        }

        public async Task<(bool exito, string mensaje, bool requiere2FA)> IniciarSesionAsync(string correo, string password)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(correo))
                    return (false, "El correo es requerido", false);

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "La contraseña es requerida", false);

                // Iniciar sesión en Firebase
                var credential = await _authClient.SignInWithEmailAndPasswordAsync(correo, password);

                if (credential?.User == null)
                    return (false, "Usuario o contraseña incorrectos", false);

                // Guardar credencial actual
                _usuarioActual = credential;

                // Actualizar último acceso en SQLite local y comprobar 2FA
                var usuario = await _databaseService.GetUsuarioByCorreoAsync(correo);
                if (usuario != null)
                {
                    usuario.UltimoAcceso = DateTime.Now;
                    await _databaseService.SaveUsuarioAsync(usuario);

                    if (usuario.Tiene2FA && !string.IsNullOrEmpty(usuario.TotpSecret))
                        return (true, "Se requiere verificación 2FA", true);
                }

                return (true, "Inicio de sesión exitoso", false);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ObtenerMensajeError(ex), false);
            }
            catch (Exception ex)
            {
                return (false, $"Error al iniciar sesión: {ex.Message}", false);
            }
        }

        public void CerrarSesion()
        {
            _authClient.SignOut();
            _usuarioActual = null;
        }

        /// <summary>
        /// Envía un correo de restablecimiento de contraseña. No requiere sesión activa.
        /// </summary>
        public async Task<(bool exito, string mensaje)> EnviarResetPasswordAsync(string correo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correo))
                    return (false, "Introduce tu correo electrónico");

                await _authClient.ResetEmailPasswordAsync(correo);
                return (true, "Se ha enviado un correo con las instrucciones para restablecer tu contraseña");
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ObtenerMensajeError(ex));
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario autenticado reautenticando primero con la contraseña actual.
        /// </summary>
        public async Task<(bool exito, string mensaje)> CambiarPasswordAsync(string passwordActual, string passwordNueva)
        {
            try
            {
                if (_usuarioActual?.User == null)
                    return (false, "No hay sesión activa");

                if (string.IsNullOrWhiteSpace(passwordActual))
                    return (false, "Introduce tu contraseña actual");

                if (string.IsNullOrWhiteSpace(passwordNueva))
                    return (false, "Introduce la nueva contraseña");

                if (passwordNueva.Length < 6)
                    return (false, "La nueva contraseña debe tener al menos 6 caracteres");

                var correo = _usuarioActual.User.Info.Email;

                // Reautenticar para confirmar la identidad antes de cambiar contraseña
                await _authClient.SignInWithEmailAndPasswordAsync(correo, passwordActual);

                // Cambiar contraseña
                await _usuarioActual.User.ChangePasswordAsync(passwordNueva);

                return (true, "Contraseña actualizada correctamente");
            }
            catch (FirebaseAuthException ex) when (ex.Reason == AuthErrorReason.WrongPassword)
            {
                return (false, "La contraseña actual es incorrecta");
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ObtenerMensajeError(ex));
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public string? ObtenerCorreoUsuario()
        {
            return _usuarioActual?.User?.Info?.Email;
        }

        public string? ObtenerUidUsuario()
        {
            return _usuarioActual?.User?.Uid;
        }

        private string ObtenerMensajeError(FirebaseAuthException ex)
        {
            return ex.Reason switch
            {
                AuthErrorReason.EmailExists => "El correo ya está registrado",
                AuthErrorReason.InvalidEmailAddress => "Correo electrónico inválido",
                AuthErrorReason.WeakPassword => "La contraseña es muy débil",
                AuthErrorReason.WrongPassword => "Usuario o contraseña incorrectos",
                AuthErrorReason.UserNotFound => "Usuario no encontrado",
                AuthErrorReason.TooManyAttemptsTryLater => "Demasiados intentos, intenta más tarde",
                AuthErrorReason.UnknownEmailAddress => "Correo no registrado",
                _ => "Usuario o contraseña incorrectos"
            };
        }
    }
}
