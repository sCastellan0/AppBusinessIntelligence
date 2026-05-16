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

        public async Task<(bool exito, string mensaje)> IniciarSesionAsync(string correo, string password)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(correo))
                    return (false, "El correo es requerido");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "La contraseña es requerida");

                // Iniciar sesión en Firebase
                var credential = await _authClient.SignInWithEmailAndPasswordAsync(correo, password);

                if (credential?.User == null)
                    return (false, "Usuario o contraseña incorrectos");

                // Guardar credencial actual
                _usuarioActual = credential;

                // Actualizar último acceso en SQLite local
                var usuario = await _databaseService.GetUsuarioByNombreAsync(correo);
                if (usuario != null)
                {
                    usuario.UltimoAcceso = DateTime.Now;
                    await _databaseService.SaveUsuarioAsync(usuario);
                }

                return (true, "Inicio de sesión exitoso");
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ObtenerMensajeError(ex));
            }
            catch (Exception ex)
            {
                return (false, $"Error al iniciar sesión: {ex.Message}");
            }
        }

        public void CerrarSesion()
        {
            _authClient.SignOut();
            _usuarioActual = null;
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
                _ => $"Error de autenticación: {ex.Message}"
            };
        }
    }
}
