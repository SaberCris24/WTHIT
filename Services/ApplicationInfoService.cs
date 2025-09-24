using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Concurrent;

namespace Plantilla.Services
{
    internal class LRUCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lockObject = new();

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
                value = default!;
                return false;
            }
        }

        public void Set(TKey key, TValue value)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    node.Value.Value = value;
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                else
                {
                    if (_cache.Count >= _capacity)
                    {
                        var last = _lruList.Last;
                        if (last != null)
                        {
                            _cache.Remove(last.Value.Key);
                            _lruList.RemoveLast();
                        }
                    }

                    var newNode = _lruList.AddFirst(new CacheItem(key, value));
                    _cache.Add(key, newNode);
                }
            }
        }

        public int Count => _cache.Count;

        public void Clear()
        {
            lock (_lockObject)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        private class CacheItem
        {
            public TKey Key { get; }
            public TValue Value { get; set; }
            public CacheItem(TKey key, TValue value) => (Key, Value) = (key, value);
        }
    }

    public interface IApplicationInfoService
    {
        string GetApplicationInfo(Process process);
        Dictionary<string, string> GetApplicationInfoBatch(IEnumerable<Process> processes);
        void ClearCache();
    }

    public class ApplicationInfoService : IApplicationInfoService
    {
        private readonly LRUCache<string, string> _processCache = new(500);
        private readonly LRUCache<string, string> _registryCache = new(200);
        private readonly LRUCache<string, string> _storeAppCache = new(100);
        
        private readonly HashSet<string> _systemProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "smss", "wininit", "services", "lsass", "winlogon", "system",
            "dwm", "explorer", "taskhost", "audiodg", "spoolsv", "conhost", "dllhost"
        };

        public string GetApplicationInfo(Process process)
        {
            if (process == null) return "Unknown";

            try
            {
                if (_processCache.TryGet(process.ProcessName, out var cachedInfo))
                {
                    return cachedInfo;
                }

                string appInfo = GetApplicationInfoInternal(process);
                _processCache.Set(process.ProcessName, appInfo);
                return appInfo;
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        public Dictionary<string, string> GetApplicationInfoBatch(IEnumerable<Process> processes)
        {
            var result = new Dictionary<string, string>();
            var uncachedProcesses = new List<Process>();

            foreach (var process in processes)
            {
                if (_processCache.TryGet(process.ProcessName, out var cachedInfo))
                {
                    result[process.ProcessName] = cachedInfo;
                }
                else
                {
                    uncachedProcesses.Add(process);
                }
            }

            if (uncachedProcesses.Count > 0)
            {
                var parallelQuery = uncachedProcesses.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount);

                var newResults = new ConcurrentDictionary<string, string>();

                parallelQuery.ForAll(process =>
                {
                    try
                    {
                        var info = GetApplicationInfoInternal(process);
                        newResults.TryAdd(process.ProcessName, info);
                        _processCache.Set(process.ProcessName, info);
                    }
                    catch
                    {
                        newResults.TryAdd(process.ProcessName, "Unknown");
                    }
                });

                foreach (var kvp in newResults)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        private string GetApplicationInfoInternal(Process process)
        {
            if (_systemProcesses.Contains(process.ProcessName))
            {
                return $"Windows {process.ProcessName}";
            }

            string appInfo = "Unknown";

            try
            {
                string processPath = process.MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(processPath))
                {
                    if (processPath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
                    {
                        appInfo = GetStoreAppName(processPath);
                    }
                    else
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                        appInfo = GetBestAppName(versionInfo, processPath);
                    }

                    var registryInfo = GetRegistryAppInfo(process.ProcessName);
                    if (!string.IsNullOrEmpty(registryInfo))
                    {
                        appInfo = registryInfo;
                    }
                }
            }
            catch
            {
                appInfo = GetRegistryAppInfo(process.ProcessName) ?? "System Process";
            }

            return appInfo;
        }

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

        private string GetStoreAppName(string processPath)
        {
            var cacheKey = processPath.GetHashCode().ToString();
            if (_storeAppCache.TryGet(cacheKey, out var cachedName))
                return cachedName;

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
                            _storeAppCache.Set(cacheKey, displayName);
                            return displayName;
                        }
                    }

                    var result = $"Store: {FormatAppName(namePart)}";
                    _storeAppCache.Set(cacheKey, result);
                    return result;
                }
            }
            catch { }

            var fallback = "Windows Store App";
            _storeAppCache.Set(cacheKey, fallback);
            return fallback;
        }

        private string? GetRegistryAppInfo(string processName)
        {
            if (_registryCache.TryGet(processName, out var cachedResult))
                return string.IsNullOrEmpty(cachedResult) ? null : cachedResult;

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

                    var keyNames = baseKey.GetSubKeyNames();
                    foreach (var keyName in keyNames)
                    {
                        using var key = baseKey.OpenSubKey(keyName);
                        if (key == null) continue;

                        var displayName = key.GetValue("DisplayName") as string;
                        var exeName = Path.GetFileNameWithoutExtension(keyName);

                        if (!string.IsNullOrEmpty(displayName) &&
                            exeName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        {
                            _registryCache.Set(processName, displayName);
                            return displayName;
                        }
                    }
                }

                _registryCache.Set(processName, string.Empty);
            }
            catch 
            {
                _registryCache.Set(processName, string.Empty);
            }

            return null;
        }

        private string FormatAppName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                name,
                "([A-Z])",
                " $1",
                System.Text.RegularExpressions.RegexOptions.Compiled
            ).Trim();
        }

        public void ClearCache()
        {
            _processCache.Clear();
            _registryCache.Clear();
            _storeAppCache.Clear();
        }

        public (int ProcessCache, int RegistryCache, int StoreAppCache) GetCacheStats()
        {
            return (_processCache.Count, _registryCache.Count, _storeAppCache.Count);
        }
    }
}