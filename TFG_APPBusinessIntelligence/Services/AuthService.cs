using System.Security.Cryptography;
using System.Text;
using TFG_APPBusinessIntelligence.Models;

namespace TFG_APPBusinessIntelligence.Services
{
    public class AuthService
    {
        private readonly DatabaseService _databaseService;
        private Usuario? _usuarioActual;

        public Usuario? UsuarioActual => _usuarioActual;
        public bool EstaAutenticado => _usuarioActual != null;

        public AuthService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<(bool exito, string mensaje)> RegistrarUsuarioAsync(
            string nombreUsuario, 
            string password, 
            string? correo = null, 
            string? telefono = null)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return (false, "El nombre de usuario es requerido");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "La contraseña es requerida");

            if (password.Length < 4)
                return (false, "La contraseña debe tener al menos 4 caracteres");

            // Verificar si el usuario ya existe
            if (await _databaseService.ExisteUsuarioAsync(nombreUsuario))
                return (false, "El nombre de usuario ya está en uso");

            // Crear nuevo usuario
            var nuevoUsuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                PasswordHash = HashPassword(password),
                Correo = correo,
                Telefono = telefono,
                FechaRegistro = DateTime.Now
            };

            try
            {
                await _databaseService.SaveUsuarioAsync(nuevoUsuario);
                return (true, "Usuario registrado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al registrar usuario: {ex.Message}");
            }
        }

        public async Task<(bool exito, string mensaje)> IniciarSesionAsync(string nombreUsuario, string password)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return (false, "El nombre de usuario es requerido");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "La contraseña es requerida");

            // Buscar usuario
            var usuario = await _databaseService.GetUsuarioByNombreAsync(nombreUsuario);

            if (usuario == null)
                return (false, "Usuario o contraseña incorrectos");

            // Verificar contraseña
            if (!VerifyPassword(password, usuario.PasswordHash))
                return (false, "Usuario o contraseña incorrectos");

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.Now;
            await _databaseService.SaveUsuarioAsync(usuario);

            // Establecer usuario actual
            _usuarioActual = usuario;

            return (true, "Inicio de sesión exitoso");
        }

        public void CerrarSesion()
        {
            _usuarioActual = null;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            string hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
