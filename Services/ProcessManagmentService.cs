using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Plantilla.Models;

namespace Plantilla.Services
{
    public interface IProcessManagementService
    {
        List<ProcessItem> GetAllProcesses();
        Task RefreshProcessesAsync();
        Process? GetProcessById(int processId);
        string GetProcessPath(Process process);
        bool IsProcessAccessible(Process process);
    }

    public class ProcessManagementService : IProcessManagementService
    {
        private readonly IApplicationInfoService _applicationInfoService;
        
        public ProcessManagementService(IApplicationInfoService applicationInfoService)
        {
            _applicationInfoService = applicationInfoService;
        }

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

        public async Task RefreshProcessesAsync()
        {
            await Task.Run(() => Process.GetProcesses());
        }

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