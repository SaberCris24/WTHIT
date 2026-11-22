using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WTHIT.Core;
using WTHIT.Models;

namespace WTHIT.Services
{
    /// <summary>
    /// Persists completed VirusTotal reports for unchanged executable files.
    /// A result is restored only when its path, size, and last-write timestamp still match.
    /// </summary>
    public sealed class ScanHistoryService
    {
        private static readonly Lazy<ScanHistoryService> _instance = new(() => new ScanHistoryService());
        private readonly object _syncRoot = new();
        private readonly string _historyPath;
        private List<ScanHistoryEntry>? _entries;

        public static ScanHistoryService Shared => _instance.Value;

        private ScanHistoryService()
        {
            var folder = Configuration.LocalDataDirectory;
            Directory.CreateDirectory(folder);
            _historyPath = Path.Combine(folder, "scan-history.json");
        }

        public Task SaveAsync(string filePath, VirusTotalScanResult result) =>
            Task.Run(() => Save(filePath, result));

        public Task ClearAsync() => Task.Run(Clear);

        /// <summary>Restores matching cached reports and skips process lookup when history is empty.</summary>
        public void RestoreForProcesses(IEnumerable<ProcessItem> processes, ProcessManagementService processService,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, ScanHistoryEntry> reports;
            lock (_syncRoot)
            {
                var entries = GetEntries();
                if (entries.Count == 0)
                    return;

                reports = new Dictionary<string, ScanHistoryEntry>(entries.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                    reports.TryAdd(entry.FilePath, entry);
            }

            foreach (var process in processes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var runningProcess = processService.GetProcessById(process.ProcessId);
                    if (runningProcess == null) continue;

                    var filePath = processService.GetProcessPath(runningProcess);
                    if (string.IsNullOrWhiteSpace(filePath) ||
                        !reports.TryGetValue(Path.GetFullPath(filePath), out var entry) ||
                        entry.Result is not { HasReport: true } savedResult)
                        continue;

                    var file = new FileInfo(entry.FilePath);
                    if (file.Length != entry.FileLength || file.LastWriteTimeUtc != entry.LastWriteTimeUtc)
                        continue;

                    process.FilePath = filePath;
                    process.ScanDetails = savedResult;
                    process.VirusStatus = "Scanned";
                    process.IsScanned = true;
                }
                catch
                {
                    // Some system processes do not expose their executable path.
                }
            }
        }

        private void Save(string filePath, VirusTotalScanResult result)
        {
            if (!result.HasReport ||
                !TryCreateFingerprint(filePath, out var normalizedPath, out var length, out var lastWriteTimeUtc))
                return;

            try
            {
                lock (_syncRoot)
                {
                    var entries = GetEntries();
                    entries.RemoveAll(entry => string.Equals(entry.FilePath, normalizedPath, StringComparison.OrdinalIgnoreCase));
                    entries.Add(new ScanHistoryEntry
                    {
                        FilePath = normalizedPath,
                        FileLength = length,
                        LastWriteTimeUtc = lastWriteTimeUtc,
                        SavedAtUtc = DateTime.UtcNow,
                        Result = result
                    });

                    _entries = entries
                        .OrderByDescending(entry => entry.SavedAtUtc)
                        .Take(Configuration.MaxScanResults)
                        .ToList();

                    File.WriteAllText(_historyPath, JsonSerializer.Serialize(_entries));
                }
            }
            catch
            {
                // A report was obtained successfully; an unavailable local cache must not
                // turn that completed scan into an error for the user.
            }
        }

        private void Clear()
        {
            try
            {
                lock (_syncRoot)
                {
                    _entries?.Clear();
                    if (File.Exists(_historyPath))
                        File.Delete(_historyPath);
                }
            }
            catch
            {
                // A cache write failure should not interrupt the scan.
            }
        }

        private List<ScanHistoryEntry> GetEntries()
        {
            if (_entries != null) return _entries;

            try
            {
                var savedEntries = File.Exists(_historyPath)
                    ? JsonSerializer.Deserialize<List<ScanHistoryEntry>>(File.ReadAllText(_historyPath))
                    : null;
                _entries = savedEntries?
                    .Where(entry => entry.Result is { HasReport: true })
                    .OrderByDescending(entry => entry.SavedAtUtc)
                    .Take(Configuration.MaxScanResults)
                    .ToList() ?? new List<ScanHistoryEntry>();
            }
            catch
            {
                _entries = new List<ScanHistoryEntry>();
            }

            return _entries;
        }

        private static bool TryCreateFingerprint(string filePath, out string normalizedPath, out long length, out DateTime lastWriteTimeUtc)
        {
            normalizedPath = string.Empty;
            length = 0;
            lastWriteTimeUtc = default;

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

                var info = new FileInfo(filePath);
                normalizedPath = Path.GetFullPath(info.FullName);
                length = info.Length;
                lastWriteTimeUtc = info.LastWriteTimeUtc;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class ScanHistoryEntry
        {
            public string FilePath { get; set; } = string.Empty;
            public long FileLength { get; set; }
            public DateTime LastWriteTimeUtc { get; set; }
            public DateTime SavedAtUtc { get; set; }
            public VirusTotalScanResult? Result { get; set; }
        }
    }
}
