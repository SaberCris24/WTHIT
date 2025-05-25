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
        /// <param name="processName">Name of the process to look up</param>
        /// <returns>Process information if found, null otherwise</returns>
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