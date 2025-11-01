using System.Collections.Generic;
using System.Linq;
using Plantilla.Models;

namespace Plantilla.Services
{
    /// <summary>
    /// Service that handles the selection and state of processes in the application.
    /// Centralizes the selection logic and maintains a consistent state.
    /// </summary>
    public interface IProcessSelectionService
    {
        /// <summary>
        /// Checks if there are any selected processes in the provided collection
        /// </summary>
        bool HasSelectedProcesses(IEnumerable<ProcessItem> processes);

        /// <summary>
        /// Gets all selected processes from the collection
        /// </summary>
        List<ProcessItem> GetSelectedProcesses(IEnumerable<ProcessItem> processes);

        /// <summary>
        /// Updates the selection state of a specific process
        /// </summary>
        void UpdateProcessSelection(ProcessItem process, bool isSelected);

        /// <summary>
        /// Clears all selections in the process collection
        /// </summary>
        void ClearSelections(IEnumerable<ProcessItem> processes);
        
        /// <summary>
        /// Synchronizes the selection state between two process collections
        /// </summary>
        void SyncSelectionState(IEnumerable<ProcessItem> source, IEnumerable<ProcessItem> target);
    }

    public class ProcessSelectionService : IProcessSelectionService
    {
        /// <summary>
        /// Checks if there are any selected processes in the collection
        /// </summary>
        /// <param name="processes">Collection of processes to check</param>
        /// <returns>True if there is at least one selected process</returns>
        public bool HasSelectedProcesses(IEnumerable<ProcessItem> processes)
        {
            return processes?.Any(p => p.IsSelected) ?? false;
        }

        /// <summary>
        /// Gets the list of selected processes
        /// </summary>
        /// <param name="processes">Collection of processes to filter</param>
        /// <returns>List of selected processes</returns>
        public List<ProcessItem> GetSelectedProcesses(IEnumerable<ProcessItem> processes)
        {
            return processes?.Where(p => p.IsSelected).ToList() ?? new List<ProcessItem>();
        }

        /// <summary>
        /// Updates the selection state of a process
        /// </summary>
        /// <param name="process">Process to update</param>
        /// <param name="isSelected">New selection state</param>
        public void UpdateProcessSelection(ProcessItem process, bool isSelected)
        {
            if (process != null)
            {
                process.IsSelected = isSelected;
            }
        }

        /// <summary>
        /// Clears all selections in the collection
        /// </summary>
        /// <param name="processes">Collection of processes to clear</param>
        public void ClearSelections(IEnumerable<ProcessItem> processes)
        {
            if (processes == null) return;

            foreach (var process in processes)
            {
                process.IsSelected = false;
            }
        }

        /// <summary>
        /// Synchronizes the selection state between two collections
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="target">Target collection</param>
        public void SyncSelectionState(IEnumerable<ProcessItem> source, IEnumerable<ProcessItem> target)
        {
            if (source == null || target == null) return;

            var selectedIds = source.Where(p => p.IsSelected)
                                  .Select(p => p.ProcessId)
                                  .ToHashSet();

            foreach (var process in target)
            {
                process.IsSelected = selectedIds.Contains(process.ProcessId);
            }
        }
    }
}