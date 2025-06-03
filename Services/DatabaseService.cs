using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    /// <summary>
    /// Manages SQLite database operations for process information
    /// </summary>
    public class DatabaseService
    {
        // Database connection instance
        private static SQLiteAsyncConnection? _database;
        private static readonly string dbName = "ProcessInfo.db";

        /// <summary>
        /// Sets up the database connection
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_database != null)
                return;

            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string databasePath = Path.Combine(appDirectory, dbName);

                _database = new SQLiteAsyncConnection(databasePath);
                await _database.CreateTableAsync<ProcessInfo>(); 
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets process information from the database
        /// </summary>
        public async Task<ProcessInfo?> GetProcessInfoAsync(string processName)
        {
            try
            {
                await InitializeAsync();
                
                if (_database == null)
                    throw new InvalidOperationException("Database not initialized");

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