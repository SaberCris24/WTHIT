using System;
using System.Collections.Generic;
using System.Linq;
using WTHIT.Models;

namespace WTHIT.Services
{
    /// <summary>
    /// Service for searching and sorting processes - highly optimized with Span and caching
    /// </summary>
    public sealed class ProcessSearchService
    {
        private static readonly Lazy<ProcessSearchService> _instance = new(() => new ProcessSearchService());
        public static ProcessSearchService Shared => _instance.Value;

        private static readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;
        private readonly Func<ProcessItem, string, bool> _searchPredicate;
        private readonly Core.ExpiringCache<string, List<string>> _suggestionCache;

        private ProcessSearchService()
        {
            _suggestionCache = new Core.ExpiringCache<string, List<string>>(TimeSpan.FromSeconds(30), 50);
            _searchPredicate = (p, s) =>
                ContainsIgnoreCase(p.ProcessName, s) ||
                (p.ApplicationRelated != null && ContainsIgnoreCase(p.ApplicationRelated, s));
        }

        private static bool ContainsIgnoreCase(string text, string search)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
                return false;

            return text.AsSpan().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public IEnumerable<ProcessItem> SearchProcesses(IEnumerable<ProcessItem> processes, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return processes;

            var results = processes.Where(p => _searchPredicate(p, searchText));
            return results;
        }

        public List<string> GetSearchSuggestions(IEnumerable<ProcessItem> processes, string searchText, int maxSuggestions = 5)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<string>(0);

            if (_suggestionCache.TryGet(searchText, out var cached))
            {
                return cached;
            }

            var suggestions = processes
                .Where(p => ContainsIgnoreCase(p.ProcessName, searchText))
                .Select(p => p.ProcessName)
                .Distinct(_comparer)
                .Take(maxSuggestions)
                .ToList();

            _suggestionCache.Set(searchText, suggestions, TimeSpan.FromSeconds(30));
            return suggestions;
        }

        public IEnumerable<ProcessItem> SortProcesses(IEnumerable<ProcessItem> processes, string sortBy, bool ascending)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "name" when ascending => processes.OrderBy(p => p.ProcessName, _comparer),
                "name" => processes.OrderByDescending(p => p.ProcessName, _comparer),
                "id" when ascending => processes.OrderBy(p => p.ProcessId),
                "id" => processes.OrderByDescending(p => p.ProcessId),
                "application" when ascending => processes.OrderBy(p => p.ApplicationRelated ?? string.Empty, _comparer),
                "application" => processes.OrderByDescending(p => p.ApplicationRelated ?? string.Empty, _comparer),
                _ when ascending => processes.OrderBy(p => p.ProcessName, _comparer),
                _ => processes.OrderByDescending(p => p.ProcessName, _comparer)
            };
        }
    }
}
