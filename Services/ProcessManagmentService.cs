using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WTHIT.Models;

namespace WTHIT.Services
{
    /// <summary>
    /// Retrieves running processes and their executable paths.
    /// </summary>
    public sealed class ProcessManagementService : IDisposable
    {
        private readonly ApplicationInfoService _applicationInfoService;
        private bool _disposed;

        public ProcessManagementService(ApplicationInfoService applicationInfoService)
        {
            _applicationInfoService = applicationInfoService;
        }

        /// <summary>
        /// Gets all accessible processes.
        /// Each Process object is disposed as soon as its data is read.
        /// </summary>
        public List<ProcessItem> GetAllProcesses()
        {
            var processes = new List<ProcessItem>();

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    processes.Add(new ProcessItem
                    {
                        ProcessName = process.ProcessName,
                        ProcessId = process.Id,
                        ApplicationRelated = _applicationInfoService.GetApplicationInfo(process),
                        VirusStatus = "Not Scanned",
                        Information = "Click to view details",
                        IsSelected = false
                    });
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    // Process is inaccessible or already terminated.
                    Debug.WriteLine($"Skipping inaccessible process: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error reading process: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }

            return processes;
        }

        /// <summary>
        /// Gets all processes asynchronously.
        /// </summary>
        public async Task<List<ProcessItem>> GetAllProcessesAsync() =>
            await Task.Run(() => GetAllProcesses());

        /// <summary>
        /// Kept for compatibility, but no longer needed.
        /// The page refresh logic already reloads processes correctly.
        /// </summary>
        public Task RefreshProcessesAsync() => Task.CompletedTask;

        /// <summary>
        /// Gets a process by ID. Caller is responsible for disposing the returned process.
        /// </summary>
        public Process? GetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the executable path for a process.
        /// Queries the path with limited process access before trying the main module.
        /// </summary>
        public string GetProcessPath(Process process)
        {
            if (process == null)
                return string.Empty;

            try
            {
                var path = ProcessIconExtractor.GetImagePath(process.Id);
                if (!string.IsNullOrWhiteSpace(path))
                    return path;

                return process.MainModule?.FileName ?? string.Empty;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks whether a process is accessible enough to read basic module info.
        /// </summary>
        public bool IsProcessAccessible(Process process)
        {
            try
            {
                return process.MainModule != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes the service.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}