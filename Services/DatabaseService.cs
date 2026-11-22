using SQLite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WTHIT.Models;

namespace WTHIT.Services
{
    /// <summary>
    /// Reads process descriptions from the SQLite database shipped with the application.
    /// </summary>
    public sealed class DatabaseService : IDisposable, IAsyncDisposable
    {
        private static readonly Lazy<DatabaseService> _instance = new(() => new DatabaseService());
        private const string DbName = "ProcessInfo.db";

        private SQLiteAsyncConnection? _database;
        private bool _initialized;
        private bool _disposed;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private DatabaseService() { }

        public static DatabaseService Instance => _instance.Value;

        public async Task InitializeAsync()
        {
            if (_initialized && !_disposed) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized && !_disposed) return;
                if (_disposed) return;

                var databasePath = GetDatabasePath();
                _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.FullMutex);
                await _database.ExecuteScalarAsync<int>("SELECT 1 FROM ProcessInfo LIMIT 1");
                _initialized = true;

                System.Diagnostics.Debug.WriteLine($"Database initialized from: {databasePath}");
            }
            finally
            {
                _initLock.Release();
            }
        }

        private static string GetDatabasePath()
        {
            return Path.Combine(AppContext.BaseDirectory, DbName);
        }

        public async Task<ProcessInfo?> GetProcessInfoAsync(string processName)
        {
            if (_disposed) return null;

            try
            {
                await InitializeAsync();

                if (_database == null || _disposed)
                    return null;

                return await _database.Table<ProcessInfo>()
                    .FirstOrDefaultAsync(p => p.ProcessName.ToLower() == processName.ToLower());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting process info: {ex.Message}");
                return null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            try
            {
                if (_database != null)
                {
                    try
                    {
                        await _database.CloseAsync();
                    }
                    catch { }
                    _database = null;
                }
            }
            finally
            {
                _initLock.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_database != null)
                {
                    try
                    {
                        _database.GetConnection().Close();
                    }
                    catch { }
                    _database = null;
                }
            }
            finally
            {
                _initLock.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void DisposeSync()
        {
            Dispose();
        }
    }
}
