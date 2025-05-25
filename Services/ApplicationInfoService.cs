using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Linq;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for getting application information from processes
    /// </summary>
    public interface IApplicationInfoService
    {
        /// <summary>
        /// Gets information about an application from its process
        /// </summary>
        string GetApplicationInfo(Process process);
    }

    /// <summary>
    /// Service for retrieving application information from processes
    /// </summary>
    public class ApplicationInfoService : IApplicationInfoService
    {
        // Cache for storing process information
        private readonly Dictionary<string, string> _processCache = new Dictionary<string, string>();
        
        // List of known system processes
        private readonly HashSet<string> _systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "smss", "wininit", "services", "lsass", "winlogon", "system"
        };

        /// <summary>
        /// Gets information about an application from its process
        /// </summary>
        public string GetApplicationInfo(Process process)
        {
            try
            {
                // Check cache first
                if (_processCache.TryGetValue(process.ProcessName, out string cachedInfo))
                {
                    return cachedInfo;
                }

                // For system processes, return quickly
                if (_systemProcesses.Contains(process.ProcessName))
                {
                    var result = $"Windows {process.ProcessName}";
                    _processCache[process.ProcessName] = result;
                    return result;
                }

                string appInfo = "Unknown";

                try
                {
                    string processPath = process.MainModule?.FileName ?? string.Empty;
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        // Try to get info from registry for Store Apps
                        if (processPath.Contains("WindowsApps"))
                        {
                            appInfo = GetStoreAppName(processPath);
                        }
                        else
                        {
                            // Get info from executable
                            var versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                            appInfo = GetBestAppName(versionInfo, processPath);
                        }

                        // Try to get additional info from registry
                        var registryInfo = GetRegistryAppInfo(process.ProcessName);
                        if (!string.IsNullOrEmpty(registryInfo))
                        {
                            appInfo = registryInfo;
                        }
                    }
                }
                catch
                {
                    // If process access fails, try registry
                    appInfo = GetRegistryAppInfo(process.ProcessName) ?? "System Process";
                }

                // Save to cache
                _processCache[process.ProcessName] = appInfo;
                return appInfo;
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the best available name for an application from its version info
        /// </summary>
        private string GetBestAppName(FileVersionInfo versionInfo, string processPath)
        {
            return new[]
            {
                versionInfo.ProductName,
                versionInfo.FileDescription,
                versionInfo.CompanyName,
                Path.GetFileNameWithoutExtension(processPath)
            }
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown";
        }

        /// <summary>
        /// Gets the name of a Windows Store application
        /// </summary>
        private string GetStoreAppName(string processPath)
        {
            try
            {
                var pathParts = processPath.Split('\\');
                var appIndex = Array.FindIndex(pathParts, x => x.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase));
                if (appIndex >= 0 && pathParts.Length > appIndex + 1)
                {
                    var appPart = pathParts[appIndex + 1];
                    var namePart = appPart.Split('_')[0];

                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Families\" + namePart))
                    {
                        if (key?.GetValue("DisplayName") is string displayName)
                        {
                            return displayName;
                        }
                    }

                    return $"Store: {FormatAppName(namePart)}";
                }
            }
            catch { }

            return "Windows Store App";
        }

        /// <summary>
        /// Gets application information from the registry
        /// </summary>
        private string? GetRegistryAppInfo(string processName)
        {
            try
            {
                var registryPaths = new[]
                {
                    @"Software\Microsoft\Windows\CurrentVersion\App Paths\",
                    @"Software\Microsoft\Windows\CurrentVersion\Uninstall\",
                    @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\"
                };

                foreach (var basePath in registryPaths)
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(basePath);
                    if (baseKey == null) continue;

                    foreach (var keyName in baseKey.GetSubKeyNames())
                    {
                        using var key = baseKey.OpenSubKey(keyName);
                        if (key == null) continue;

                        var displayName = key.GetValue("DisplayName") as string;
                        var exeName = Path.GetFileNameWithoutExtension(keyName);

                        if (!string.IsNullOrEmpty(displayName) &&
                            exeName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        {
                            return displayName;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Formats an application name by adding spaces before capital letters
        /// </summary>
        private string FormatAppName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                name,
                "([A-Z])",
                " $1",
                System.Text.RegularExpressions.RegexOptions.Compiled
            ).Trim();
        }
    }
}