using System;
using System.Collections.Concurrent;
using System.Threading;

namespace WTHIT.Core
{
    /// <summary>
    /// Stores a limited number of entries with individual expiration times.
    /// </summary>
    public sealed class ExpiringCache<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheEntry> _cache = new();
        private readonly TimeSpan _defaultExpiration;
        private readonly int _maxSize;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public ExpiringCache(TimeSpan? expiration = null, int maxSize = 500)
        {
            _defaultExpiration = expiration ?? TimeSpan.FromMinutes(5);
            _maxSize = maxSize;
            _cleanupTimer = new Timer(CleanupExpired, null, _defaultExpiration, _defaultExpiration);
        }

        public bool TryGet(TKey key, out TValue value)
        {
            value = default!;
            if (_disposed) return false;

            if (!_cache.TryGetValue(key, out var entry) || entry.IsExpired)
                return false;

            _cache.TryUpdate(key, entry.Touch(), entry);
            value = entry.Value;
            return true;
        }

        public void Set(TKey key, TValue value, TimeSpan? expiration = null)
        {
            if (_disposed) return;

            if (_cache.Count >= _maxSize)
            {
                RemoveOldest();
            }

            var exp = expiration ?? _defaultExpiration;
            _cache[key] = new CacheEntry(value, exp);
        }

        public void Remove(TKey key)
        {
            if (_disposed) return;
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            if (_disposed) return;
            _cache.Clear();
        }

        public int Count => _cache.Count;

        private void RemoveOldest()
        {
            var oldest = DateTime.MaxValue;
            var oldestKey = default(TKey);

            foreach (var kvp in _cache)
            {
                if (kvp.Value.LastAccessedAt < oldest)
                {
                    oldest = kvp.Value.LastAccessedAt;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey != null)
            {
                _cache.TryRemove(oldestKey, out _);
            }
        }

        private void CleanupExpired(object? state)
        {
            if (_disposed) return;

            var now = DateTime.UtcNow;
            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _cache.TryRemove(kvp);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cleanupTimer.Dispose();
            _cache.Clear();
        }

        private readonly struct CacheEntry
        {
            public TValue Value { get; }
            public DateTime LastAccessedAt { get; }
            public DateTime ExpiresAt { get; }
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

            public CacheEntry(TValue value, TimeSpan expiration)
            {
                Value = value;
                LastAccessedAt = DateTime.UtcNow;
                ExpiresAt = LastAccessedAt + expiration;
            }

            private CacheEntry(TValue value, DateTime accessedAt, DateTime expiresAt)
            {
                Value = value;
                LastAccessedAt = accessedAt;
                ExpiresAt = expiresAt;
            }

            public CacheEntry Touch() => new(Value, DateTime.UtcNow, ExpiresAt);
        }
    }

}
