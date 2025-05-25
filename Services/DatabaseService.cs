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

        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string databasePath = Path.Combine(appDirectory, dbName);

                _database = new SQLiteAsyncConnection(databasePath);
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