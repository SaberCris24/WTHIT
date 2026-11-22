using System;
using System.Collections.Generic;
using System.Linq;
using WTHIT.Models;

namespace WTHIT.Services
{
    /// <summary>
    /// Service for managing process selection state - optimized singleton
    /// </summary>
    public sealed class ProcessSelectionService
    {
        private static readonly Lazy<ProcessSelectionService> _instance = new(() => new ProcessSelectionService());
        public static ProcessSelectionService Shared => _instance.Value;

        private ProcessSelectionService() { }

        public bool HasSelectedProcesses(IEnumerable<ProcessItem> processes) =>
            processes?.Any(p => p.IsSelected) ?? false;

        public List<ProcessItem> GetSelectedProcesses(IEnumerable<ProcessItem> processes) =>
            processes?.Where(p => p.IsSelected).ToList() ?? new List<ProcessItem>();

        public void UpdateProcessSelection(ProcessItem process, bool isSelected)
        {
            if (process != null)
                process.IsSelected = isSelected;
        }

        public void ClearSelections(IEnumerable<ProcessItem> processes)
        {
            if (processes == null) return;
            foreach (var process in processes)
                process.IsSelected = false;
        }

        public void SyncSelectionState(IEnumerable<ProcessItem> source, IEnumerable<ProcessItem> target)
        {
            if (source == null || target == null) return;

            var selectedIds = source.Where(p => p.IsSelected)
                                  .Select(p => p.ProcessId)
                                  .ToHashSet();

            foreach (var process in target)
                process.IsSelected = selectedIds.Contains(process.ProcessId);
        }
    }
}
