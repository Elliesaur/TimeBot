using System;
using System.Collections.Generic;

namespace TinyTime
{
    public class Cache<TKey, TValue>
    {
        #region Fields

        private readonly Dictionary<TKey, CacheItem<TValue>> _cache = new Dictionary<TKey, CacheItem<TValue>>();

        #endregion

        #region Public Methods

        public void Store(TKey key, TValue value, TimeSpan expiresAfter)
        {
            _cache[key] = new CacheItem<TValue>(value, expiresAfter);
        }

        public TValue Get(TKey key)
        {
            if (!_cache.ContainsKey(key)) return default;
            CacheItem<TValue> cached = _cache[key];
            if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
            {
                _cache.Remove(key);
                return default;
            }

            return cached.Value;
        }

        #endregion
    }
}