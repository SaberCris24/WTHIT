using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Plantilla.Pages.Home;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for system information service that provides process and system data
    /// </summary>
    public interface ISystemInfoService
    {
        /// <summary>
        /// Gets the top processes by resource usage
        /// </summary>
        /// <param name="count">Number of top processes to retrieve</param>
        /// <returns>List of top processes ordered by resource usage</returns>
        Task<List<TopProcessItem>> GetTopProcessesByResourceAsync(int count);
        
        /// <summary>
        /// Gets basic system information
        /// </summary>
        /// <returns>System information including process count</returns>
        Task<SystemInfoItem> GetSystemInfoAsync();

        /// <summary>
        /// Clears the internal cache
        /// </summary>
        void ClearCache();
        
        /// <summary>
        /// Gets statistics about the service
        /// </summary>
        /// <returns>Tuple containing process history count and last update time</returns>
        (int ProcessHistoryCount, DateTime LastUpdate) GetStats();
    }

    /// <summary>
    /// Service implementation for retrieving system information and process data
    /// </summary>
    public class SystemInfoService : ISystemInfoService
    {
        /// <summary>
        /// Dictionary to track CPU usage history for processes
        /// </summary>
        private Dictionary<int, ProcessCpuInfo> _processCpuHistory = new Dictionary<int, ProcessCpuInfo>();
        
        /// <summary>
        /// Last time the system information was updated
        /// </summary>
        private DateTime _lastUpdate = DateTime.Now;

        /// <summary>
        /// Internal class to store CPU usage information for a process
        /// </summary>
        private class ProcessCpuInfo
        {
            /// <summary>
            /// Last recorded total processor time for the process
            /// </summary>
            public TimeSpan LastTotalProcessorTime { get; set; }
            
            /// <summary>
            /// Last time the process information was updated
            /// </summary>
            public DateTime LastUpdateTime { get; set; }
        }

        /// <summary>
        /// Gets the top processes by resource usage (CPU and memory)
        /// </summary>
        /// <param name="count">Number of top processes to retrieve</param>
        /// <returns>List of top processes ordered by resource usage</returns>
        public async Task<List<TopProcessItem>> GetTopProcessesByResourceAsync(int count)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var processes = Process.GetProcesses();
                    var processInfoList = new List<(string name, long memory, double cpuPercent, int id)>();

                    var currentTime = DateTime.Now;
                    var timeSinceLastUpdate = (currentTime - _lastUpdate).TotalMilliseconds;
                    _lastUpdate = currentTime;

                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.HasExited) continue;
                            long memoryUsage = process.WorkingSet64;
                            double cpuUsage = CalculateProcessCpuUsage(process, timeSinceLastUpdate);
                            processInfoList.Add((process.ProcessName, memoryUsage, cpuUsage, process.Id));
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var topProcesses = processInfoList
                        .OrderByDescending(p => (p.cpuPercent * 2) + (p.memory / (1024.0 * 1024.0 * 1024.0)))
                        .Take(count)
                        .Select(p => new TopProcessItem
                        {
                            ProcessId = p.id,
                            ProcessName = p.name,
                            CpuUsageValue = p.cpuPercent,
                            CpuUsage = $"{p.cpuPercent:F1}%",
                            MemoryUsageValue = p.memory,
                            MemoryUsage = FormatBytes(p.memory)
                        })
                        .ToList();

                    CleanupOldProcessHistory(processes.Select(p => p.Id).ToHashSet());
                    return topProcesses;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting top processes: {ex.Message}");
                    return new List<TopProcessItem>();
                }
            });
        }

        /// <summary>
        /// Calculates the CPU usage percentage for a process
        /// </summary>
        /// <param name="process">The process to calculate CPU usage for</param>
        /// <param name="timeSinceLastUpdate">Time elapsed since last update in milliseconds</param>
        /// <returns>CPU usage percentage</returns>
        private double CalculateProcessCpuUsage(Process process, double timeSinceLastUpdate)
        {
            try
            {
                var currentTotalProcessorTime = process.TotalProcessorTime;
                var processId = process.Id;

                if (_processCpuHistory.ContainsKey(processId))
                {
                    var previousInfo = _processCpuHistory[processId];
                    var timeDiff = (currentTotalProcessorTime - previousInfo.LastTotalProcessorTime).TotalMilliseconds;

                    double cpuUsage = 0;
                    if (timeSinceLastUpdate > 0)
                    {
                        cpuUsage = (timeDiff / timeSinceLastUpdate / Environment.ProcessorCount) * 100.0;
                        cpuUsage = Math.Min(Math.Max(cpuUsage, 0), 100);
                    }

                    _processCpuHistory[processId] = new ProcessCpuInfo
                    {
                        LastTotalProcessorTime = currentTotalProcessorTime,
                        LastUpdateTime = DateTime.Now
                    };

                    return cpuUsage;
                }
                else
                {
                    _processCpuHistory[processId] = new ProcessCpuInfo
                    {
                        LastTotalProcessorTime = currentTotalProcessorTime,
                        LastUpdateTime = DateTime.Now
                    };
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Removes process history for processes that no longer exist or are outdated
        /// </summary>
        /// <param name="currentProcessIds">Set of current process IDs</param>
        private void CleanupOldProcessHistory(HashSet<int> currentProcessIds)
        {
            var idsToRemove = _processCpuHistory.Keys
                .Where(id => !currentProcessIds.Contains(id))
                .ToList();

            foreach (var id in idsToRemove)
            {
                _processCpuHistory.Remove(id);
            }

            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            var oldIds = _processCpuHistory
                .Where(kvp => kvp.Value.LastUpdateTime < oneMinuteAgo)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in oldIds)
            {
                _processCpuHistory.Remove(id);
            }
        }

        /// <summary>
        /// Gets basic system information
        /// </summary>
        /// <returns>System information including process count</returns>
        public async Task<SystemInfoItem> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var totalProcesses = Process.GetProcesses().Length;

                    return new SystemInfoItem
                    {
                        TotalProcesses = totalProcesses
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting system info: {ex.Message}");
                    return new SystemInfoItem
                    {
                        TotalProcesses = 0
                    };
                }
            });
        }

        /// <summary>
        /// Formats bytes into a human-readable string with appropriate unit
        /// </summary>
        /// <param name="bytes">Number of bytes to format</param>
        /// <returns>Formatted string with appropriate unit</returns>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:F1} {sizes[order]}";
        }

        /// <summary>
        /// Clears the internal cache of process CPU history
        /// </summary>
        public void ClearCache()
        {
            _processCpuHistory.Clear();
            _lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Gets statistics about the service
        /// </summary>
        /// <returns>Tuple containing process history count and last update time</returns>
        public (int ProcessHistoryCount, DateTime LastUpdate) GetStats()
        {
            return (_processCpuHistory.Count, _lastUpdate);
        }
    }
}