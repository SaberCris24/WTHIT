using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private static readonly string dbName = "ProcessInfo.db";
        // Incrementar este número cada vez que se modifique la info.
        private const int CURRENT_DATA_VERSION = 2;
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
                await _database.CreateTableAsync<VersionInfo>();

                // Crear tabla de procesos si no existe
                await _database.CreateTableAsync<ProcessInfo>();

                // Verificar versión
                var versionInfo = await _database.Table<VersionInfo>()
                    .FirstOrDefaultAsync(v => v.Key == VERSION_KEY);

                if (versionInfo == null || versionInfo.Version < CURRENT_DATA_VERSION)
                {
                    // Si no hay versión o es menor que la actual, actualizar datos
                    await UpdatePredefinedDataAsync();

                    // Actualizar versión
                    if (versionInfo == null)
                    {
                        await _database.InsertAsync(new VersionInfo
                        {
                            Key = VERSION_KEY,
                            Version = CURRENT_DATA_VERSION
                        });
                    }
                    else
                    {
                        versionInfo.Version = CURRENT_DATA_VERSION;
                        await _database.UpdateAsync(versionInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        private async Task UpdatePredefinedDataAsync()
        {
            try
            {
                // Primero eliminar todos los datos existentes
                await _database.DeleteAllAsync<ProcessInfo>();

                var predefinedProcesses = new[]
                {
                    new ProcessInfo
                    {
                        ProcessName = "chrome",
                        ApplicationRelated = "Google Chrome Web Browser",
                        Description = "Chrome.exe is a process used by Chrome Internet browser powered by Google. " +
                                   "Chrome is not an essential file to run Microsoft Windows operating system and is not required to start-up automatically."
                    },

                    new ProcessInfo
                    {
                        ProcessName = "svchost",
                        ApplicationRelated = "Windows Service Host",
                        Description = "Svchost.exe (Service Host, or SvcHost) is a system process that hosts multiple Windows services. " +
                                    "It is essential for the operation of Windows and its services. The process acts as a shell for loading services " +
                                    "from DLL files. Multiple instances of svchost.exe can run at the same time, each hosting different services. " +
                                    "This is a legitimate Windows process and should not be terminated."
                    },

                    // new ProcessInfo y seguir
                    
                };

                await _database.InsertAllAsync(predefinedProcesses);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating predefined data: {ex.Message}");
                throw;
            }
        }

        public async Task<ProcessInfo> GetProcessInfoAsync(string processName)
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