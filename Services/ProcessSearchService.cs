using System;
using System.Collections.Generic;
using System.Linq;
using Plantilla.Models;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for searching and sorting processes
    /// </summary>
    public interface IProcessSearchService
    {
        /// <summary>
        /// Searches processes based on text
        /// </summary>
        IEnumerable<ProcessItem> SearchProcesses(IEnumerable<ProcessItem> processes, string searchText);

        /// <summary>
        /// Gets search suggestions from process names
        /// </summary>
        List<string> GetSearchSuggestions(IEnumerable<ProcessItem> processes, string searchText, int maxSuggestions = 5);

        /// <summary>
        /// Sorts processes by specified criteria
        /// </summary>
        IEnumerable<ProcessItem> SortProcesses(IEnumerable<ProcessItem> processes, string sortBy, bool ascending);
    }

    /// <summary>
    /// Service for searching and sorting processes
    /// </summary>
    public class ProcessSearchService : IProcessSearchService
    {
        /// <summary>
        /// Filters processes based on search text
        /// </summary>
        public IEnumerable<ProcessItem> SearchProcesses(IEnumerable<ProcessItem> processes, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return processes;
            }

            return processes.Where(p => 
                p.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.ApplicationRelated?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true
            );
        }

        /// <summary>
        /// Gets process name suggestions based on input text
        /// </summary>
        public List<string> GetSearchSuggestions(IEnumerable<ProcessItem> processes, string searchText, int maxSuggestions = 5)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<string>();
            }

            return processes
                .Where(p => p.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ProcessName)
                .Distinct()
                .Take(maxSuggestions)
                .ToList();
        }

        /// <summary>
        /// Sorts processes by name, ID, or application
        /// </summary>
        public IEnumerable<ProcessItem> SortProcesses(IEnumerable<ProcessItem> processes, string sortBy, bool ascending)
        {
            IOrderedEnumerable<ProcessItem> sortedProcesses = sortBy.ToLower() switch
            {
                "name" => ascending
                    ? processes.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                    : processes.OrderByDescending(p => p.ProcessName, StringComparer.OrdinalIgnoreCase),
                
                "id" => ascending
                    ? processes.OrderBy(p => p.ProcessId)
                    : processes.OrderByDescending(p => p.ProcessId),
                
                "application" => ascending
                    ? processes.OrderBy(p => p.ApplicationRelated ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    : processes.OrderByDescending(p => p.ApplicationRelated ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                
                _ => ascending
                    ? processes.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                    : processes.OrderByDescending(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
            };

            return sortedProcesses;
        }
    }
}