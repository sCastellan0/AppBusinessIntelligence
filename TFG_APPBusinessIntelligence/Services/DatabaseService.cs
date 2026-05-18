using SQLite;
using TFG_APPBusinessIntelligence.Models;

namespace TFG_APPBusinessIntelligence.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "tfgapp.db3");

            // Para debug: ver la ruta en la consola
            System.Diagnostics.Debug.WriteLine($"📁 Ruta de la base de datos: {_dbPath}");
        }

        // Método público para obtener la ruta
        public string GetDatabasePath() => _dbPath;

        private async Task InitAsync()
        {
            if (_database != null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            await _database.CreateTableAsync<Usuario>();
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            await InitAsync();
            return await _database!.Table<Usuario>().ToListAsync();
        }

        public async Task<Usuario?> GetUsuarioByNombreAsync(string nombreUsuario)
        {
            await InitAsync();
            return await _database!.Table<Usuario>()
                .Where(u => u.NombreUsuario == nombreUsuario)
                .FirstOrDefaultAsync();
        }

        public async Task<Usuario?> GetUsuarioByCorreoAsync(string correo)
        {
            await InitAsync();
            return await _database!.Table<Usuario>()
                .Where(u => u.Correo == correo)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveUsuarioAsync(Usuario usuario)
        {
            await InitAsync();

            if (usuario.Id != 0)
                return await _database!.UpdateAsync(usuario);
            else
                return await _database!.InsertAsync(usuario);
        }

        public async Task<int> DeleteUsuarioAsync(Usuario usuario)
        {
            await InitAsync();
            return await _database!.DeleteAsync(usuario);
        }

        public async Task<bool> ExisteUsuarioAsync(string nombreUsuario)
        {
            await InitAsync();
            var usuario = await GetUsuarioByNombreAsync(nombreUsuario);
            return usuario != null;
        }
    }
}
