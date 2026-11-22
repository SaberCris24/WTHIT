using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace WTHIT.Services
{
    internal sealed class SimpleCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly int _capacity;
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheEntry>> _cache;
        private readonly LinkedList<CacheEntry> _lruList;
        private readonly object _lock = new();
        private readonly TimeSpan _expiration;
        private volatile bool _disposed;

        public SimpleCache(int capacity, TimeSpan? expiration = null, IEqualityComparer<TKey>? comparer = null)
        {
            _capacity = capacity;
            _expiration = expiration ?? TimeSpan.FromMinutes(10);
            _cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheEntry>>(comparer ?? EqualityComparer<TKey>.Default);
            _lruList = new LinkedList<CacheEntry>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            value = default!;
            if (_disposed) return false;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    if (node.Value.IsExpired)
                    {
                        _lruList.Remove(node);
                        _cache.TryRemove(key, out _);
                        return false;
                    }

                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
            }
            return false;
        }

        public void Set(TKey key, TValue value)
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    node.Value.Refresh(value, _expiration);
                    _lruList.AddFirst(node);
                }
                else
                {
                    if (_cache.Count >= _capacity)
                    {
                        var last = _lruList.Last;
                        if (last != null)
                        {
                            _cache.TryRemove(last.Value.Key, out _);
                            _lruList.RemoveLast();
                        }
                    }

                    var newNode = _lruList.AddFirst(new CacheEntry(key, value, _expiration));
                    _cache[key] = newNode;
                }
            }
        }

        public void Clear()
        {
            if (_disposed) return;
            lock (_lock) { _cache.Clear(); _lruList.Clear(); }
        }

        public int Count => _disposed ? 0 : _cache.Count;

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
        }

        private sealed class CacheEntry
        {
            public TKey Key { get; }
            public TValue Value { get; private set; }
            public DateTime ExpiresAt { get; private set; }
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

            public CacheEntry(TKey key, TValue value, TimeSpan expiration)
            {
                Key = key;
                Value = value;
                Refresh(value, expiration);
            }

            public void Refresh(TValue value, TimeSpan expiration)
            {
                Value = value;
                ExpiresAt = DateTime.UtcNow + expiration;
            }
        }
    }

    public sealed class ApplicationInfoService : IDisposable
    {
        private static readonly Lazy<ApplicationInfoService> _instance = new(() => new ApplicationInfoService());
        public static ApplicationInfoService Shared => _instance.Value;

        private readonly SimpleCache<string, string> _processCache = new(300, TimeSpan.FromMinutes(5), StringComparer.OrdinalIgnoreCase);
        private readonly SimpleCache<string, string> _registryCache = new(150, TimeSpan.FromMinutes(30), StringComparer.OrdinalIgnoreCase);
        private readonly SimpleCache<string, string> _storeAppCache = new(75, TimeSpan.FromMinutes(60), StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _systemProcessCache;

        private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "smss", "wininit", "services", "lsass", "winlogon", "system",
            "dwm", "explorer", "taskhost", "audiodg", "spoolsv", "conhost", "dllhost",
            "runtimebroker", "shellexperiencehost", "searchapp", "startmenuexperiencehost",
            "textinputhost", "sihost", "taskhostw", "fontdrvhost", "wudfhost", "sppsvc"
        };

        private ApplicationInfoService()
        {
            _systemProcessCache = new ConcurrentDictionary<string, string>(
                SystemProcesses.ToDictionary(p => p, p => $"Windows {p}", StringComparer.OrdinalIgnoreCase)
            );
        }

        public string GetApplicationInfo(Process process)
        {
            if (process == null) return "Unknown";

            if (_systemProcessCache.TryGetValue(process.ProcessName, out var sysInfo))
                return sysInfo;

            if (_processCache.TryGet(process.ProcessName, out var cachedInfo))
                return cachedInfo;

            var appInfo = GetApplicationInfoInternal(process);
            _processCache.Set(process.ProcessName, appInfo);
            return appInfo;
        }

        public Dictionary<string, string> GetApplicationInfoBatch(IEnumerable<Process> processes)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var process in processes)
            {
                if (_systemProcessCache.TryGetValue(process.ProcessName, out var sysInfo))
                    result[process.ProcessName] = sysInfo;
                else if (!_processCache.TryGet(process.ProcessName, out var cached))
                {
                    var info = GetApplicationInfoInternal(process);
                    result[process.ProcessName] = info;
                    _processCache.Set(process.ProcessName, info);
                }
                else
                {
                    result[process.ProcessName] = cached;
                }
            }

            return result;
        }

        private string GetApplicationInfoInternal(Process process)
        {
            string appInfo = "Unknown";

            try
            {
                string? processPath = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath))
                {
                    appInfo = processPath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)
                        ? GetStoreAppName(processPath)
                        : GetBestAppName(FileVersionInfo.GetVersionInfo(processPath), processPath);

                    var registryInfo = GetRegistryAppInfo(process.ProcessName);
                    if (!string.IsNullOrEmpty(registryInfo))
                        appInfo = registryInfo;
                }
            }
            catch
            {
                appInfo = GetRegistryAppInfo(process.ProcessName) ?? "System Process";
            }

            _processCache.Set(process.ProcessName, appInfo);
            return appInfo;
        }

        private static string GetBestAppName(FileVersionInfo versionInfo, string processPath)
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

                    using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Families\{namePart}");
                    if (key?.GetValue("DisplayName") is string displayName)
                    {
                        _storeAppCache.Set(cacheKey, displayName);
                        return displayName;
                    }

                    var result = $"Store: {FormatAppName(namePart)}";
                    _storeAppCache.Set(cacheKey, result);
                    return result;
                }
            }
            catch { }

            _storeAppCache.Set(cacheKey, "Windows Store App");
            return "Windows Store App";
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

                    foreach (var keyName in baseKey.GetSubKeyNames())
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

        private static string FormatAppName(string name) =>
            System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();

        public void ClearCache()
        {
            _processCache.Clear();
            _registryCache.Clear();
            _storeAppCache.Clear();
        }

        public (int ProcessCache, int RegistryCache, int StoreAppCache) GetCacheStats() =>
            (_processCache.Count, _registryCache.Count, _storeAppCache.Count);

        public void Dispose()
        {
            _processCache.Dispose();
            _registryCache.Dispose();
            _storeAppCache.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}