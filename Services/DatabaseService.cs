using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    public class DatabaseService
    {
        private static SQLiteAsyncConnection? _database;
        private static readonly string dbName = "ProcessInfo.db";
        // Incrementar este número cada vez que se modifique la info.
        private const int CURRENT_DATA_VERSION = 1;
        private const string VERSION_KEY = "DataVersion";

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string databasePath = Path.Combine(appDirectory, dbName);

                _database = new SQLiteAsyncConnection(databasePath);

                // Crear tabla de versión si no existe
                // await _database.CreateTableAsync<VersionInfo>();

                // Crear tabla de procesos si no existe
                //await _database.CreateTableAsync<ProcessInfo>();

                // Verificar versión
                var versionInfo = await _database.Table<VersionInfo>()
                    .FirstOrDefaultAsync(v => v.Key == VERSION_KEY);

                versionInfo.Version = CURRENT_DATA_VERSION;
                await _database.UpdateAsync(versionInfo);
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        

        public async Task<ProcessInfo?> GetProcessInfoAsync(string processName)
        {
            try
            {
                await InitializeAsync();
                return await _database.Table<ProcessInfo>()
                    .FirstOrDefaultAsync(p => p.ProcessName.ToLower() == processName.ToLower());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting process info: {ex.Message}");
                return null;
            }
        }
    }
}