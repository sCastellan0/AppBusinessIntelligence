using SQLite;

namespace TFG_APPBusinessIntelligence.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique, NotNull, MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [NotNull]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Correo { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public DateTime? UltimoAcceso { get; set; }

        public string? TotpSecret { get; set; }

        public bool Tiene2FA { get; set; } = false;
    }
}
