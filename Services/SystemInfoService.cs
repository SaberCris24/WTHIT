using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WTHIT.Services
{
    /// <summary>Samples running processes and compares CPU counters across dashboard refreshes.</summary>
    public sealed class SystemInfoService : IDisposable
    {
        public static SystemInfoService Shared { get; } = new();

        private const double CpuScoreWeight = 2.0;
        private const long BytesPerGiB = 1024L * 1024 * 1024;
        private const int BytesPerUnit = 1024;
        private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB" };

        private readonly object _sampleLock = new();
        private Dictionary<int, CpuSample> _previousSamples = new();
        private Dictionary<int, CpuSample> _currentSamples = new();
        private long _previousTimestamp;
        private DateTime _lastUpdate = DateTime.UtcNow;
        private bool _disposed;

        private SystemInfoService() { }

        /// <summary>Returns the process count and resource ranking from a single enumeration.</summary>
        public Task<DashboardSnapshot> GetDashboardSnapshotAsync(int count) => Task.Run(() =>
        {
            lock (_sampleLock)
            {
                if (_disposed)
                    return new DashboardSnapshot(0, Array.Empty<ProcessUsage>());

                try
                {
                    return ReadSnapshot(count);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting top processes: {ex.Message}");
                    return new DashboardSnapshot(0, Array.Empty<ProcessUsage>());
                }
                finally
                {
                    _currentSamples.Clear();
                }
            }
        });

        /// <summary>Contains the total process count and the highest resource consumers.</summary>
        public readonly record struct DashboardSnapshot(int TotalProcesses, IReadOnlyList<ProcessUsage> TopProcesses);

        /// <summary>Contains one ranked process and its sampled resource usage.</summary>
        public readonly record struct ProcessUsage(int Id, string Name, double Cpu, long Memory, string FormattedMemory);

        private DashboardSnapshot ReadSnapshot(int count)
        {
            var processes = Process.GetProcesses();
            long timestamp = Stopwatch.GetTimestamp();
            var ranked = new List<(int Id, string Name, double Cpu, long Memory)>(processes.Length);
            double elapsed = _previousTimestamp == 0 ? 0 : Stopwatch.GetElapsedTime(_previousTimestamp, timestamp).TotalMilliseconds;

            try
            {
                foreach (var process in processes)
                {
                    try
                    {
                        int id = process.Id;
                        string name = process.ProcessName;
                        long memory = process.WorkingSet64;
                        var sample = new CpuSample(name, process.TotalProcessorTime);
                        _currentSamples[id] = sample;
                        ranked.Add((id, name, CalculateCpuUsage(id, sample, elapsed), memory));
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                    {
                        Debug.WriteLine($"Failed to read a process: {ex.Message}");
                    }
                }
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            // Compare only consecutive samples so exited and inaccessible processes leave no CPU history.
            (_previousSamples, _currentSamples) = (_currentSamples, _previousSamples);
            _previousTimestamp = timestamp;
            _lastUpdate = DateTime.UtcNow;

            ranked.Sort((a, b) => Score(b.Cpu, b.Memory).CompareTo(Score(a.Cpu, a.Memory)));

            int topCount = Math.Clamp(count, 0, ranked.Count);
            var topProcesses = new ProcessUsage[topCount];
            for (int i = 0; i < topCount; i++)
            {
                var process = ranked[i];
                topProcesses[i] = new ProcessUsage(process.Id, process.Name, process.Cpu, process.Memory, FormatBytes(process.Memory));
            }

            return new DashboardSnapshot(processes.Length, topProcesses);
        }

        /// <summary>Computes the share of total machine CPU used since the previous sample.</summary>
        private double CalculateCpuUsage(int processId, CpuSample sample, double elapsedMilliseconds)
        {
            if (elapsedMilliseconds <= 0 || !_previousSamples.TryGetValue(processId, out var previous) ||
                !string.Equals(previous.Name, sample.Name, StringComparison.Ordinal))
                return 0;

            double consumedMilliseconds = (sample.TotalProcessorTime - previous.TotalProcessorTime).TotalMilliseconds;
            return Math.Clamp(100.0 * consumedMilliseconds / elapsedMilliseconds / Environment.ProcessorCount, 0, 100);
        }

        private static double Score(double cpu, long memory) => cpu * CpuScoreWeight + (double)memory / BytesPerGiB;

        public void ClearCache()
        {
            lock (_sampleLock)
            {
                _previousSamples.Clear();
                _currentSamples.Clear();
                _previousTimestamp = 0;
                _lastUpdate = DateTime.UtcNow;
            }
        }

        public (int ProcessHistoryCount, DateTime LastUpdate) GetStats()
        {
            lock (_sampleLock)
                return (_previousSamples.Count, _lastUpdate);
        }

        public void Dispose()
        {
            lock (_sampleLock)
            {
                if (_disposed) return;
                _disposed = true;
                ClearCache();
            }

            GC.SuppressFinalize(this);
        }

        private static string FormatBytes(long bytes)
        {
            double size = bytes;
            int unit = 0;
            while (size >= BytesPerUnit && unit < SizeUnits.Length - 1)
            {
                size /= BytesPerUnit;
                unit++;
            }

            return $"{size:F1} {SizeUnits[unit]}";
        }

        private readonly record struct CpuSample(string Name, TimeSpan TotalProcessorTime);
    }
}
