using System;
using System.Collections.Generic;
using System.Linq;
using Plantilla.Models;

namespace Plantilla.Services
{
    public interface IProcessSearchService
    {
        IEnumerable<ProcessItem> SearchProcesses(IEnumerable<ProcessItem> processes, string searchText);
        List<string> GetSearchSuggestions(IEnumerable<ProcessItem> processes, string searchText, int maxSuggestions = 5);
        IEnumerable<ProcessItem> SortProcesses(IEnumerable<ProcessItem> processes, string sortBy, bool ascending);
    }

    public class ProcessSearchService : IProcessSearchService
    {
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