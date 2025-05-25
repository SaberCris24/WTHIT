using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for managing system processes
    /// </summary>
    public interface IProcessManagementService
    {
        /// <summary>
        /// Gets all running processes
        /// </summary>
        List<ProcessItem> GetAllProcesses();

        /// <summary>
        /// Refreshes the process list
        /// </summary>
        Task RefreshProcessesAsync();

        /// <summary>
        /// Gets a process by its ID
        /// </summary>
        Process? GetProcessById(int processId);

        /// <summary>
        /// Gets the file path of a process
        /// </summary>
        string GetProcessPath(Process process);

        /// <summary>
        /// Checks if a process is accessible
        /// </summary>
        bool IsProcessAccessible(Process process);
    }

    /// <summary>
    /// Service for managing system processes
    /// </summary>
    public class ProcessManagementService : IProcessManagementService
    {
        private readonly IApplicationInfoService _applicationInfoService;
        
        /// <summary>
        /// Creates a new process management service
        /// </summary>
        public ProcessManagementService(IApplicationInfoService applicationInfoService)
        {
            _applicationInfoService = applicationInfoService;
        }

        /// <summary>
        /// Gets a list of all running processes
        /// </summary>
        public List<ProcessItem> GetAllProcesses()
        {
            var processes = new List<ProcessItem>();
            foreach (Process process in Process.GetProcesses())
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
            return processes;
        }

        /// <summary>
        /// Refreshes the list of processes
        /// </summary>
        public async Task RefreshProcessesAsync()
        {
            await Task.Run(() => Process.GetProcesses());
        }

        /// <summary>
        /// Gets a process by its ID
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
        }

        /// <summary>
        /// Gets the file path of a process
        /// </summary>
        public string GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? "Path not available";
            }
            catch (Exception)
            {
                return "Access denied";
            }
        }

        /// <summary>
        /// Checks if a process can be accessed
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
    }
}