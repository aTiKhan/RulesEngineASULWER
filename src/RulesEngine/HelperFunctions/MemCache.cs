// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RulesEngine.HelperFunctions
{
    public class MemCacheConfig
    {
        //use a simple in-memory cache with expiry and size limit. Eviction is based on expiry time (oldest first)
        public int SizeLimit { get; set; } = 1000;
    }

    internal class MemCache
    {
        private readonly MemCacheConfig _config;
        private ConcurrentDictionary<string, (object value, DateTimeOffset expiry)> _cacheDictionary;

        public MemCache(MemCacheConfig config)
        {
            if (config == null)
            {
                config = new MemCacheConfig();
            }
            _config = config;
            _cacheDictionary = new ConcurrentDictionary<string, (object value, DateTimeOffset expiry)>();
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (_cacheDictionary.TryGetValue(key, out var cacheItem))
            {
                if (cacheItem.expiry < DateTimeOffset.UtcNow)
                {
                    _cacheDictionary.TryRemove(key, out _);
                    return false;
                }
                else
                {
                    value = (T)cacheItem.value;
                    return true;
                }
            }
            return false;

        }

        public T Get<T>(string key)
        {
            TryGetValue<T>(key, out var value);
            return value;
        }

        /// <summary>
        /// Returns all known keys. May return keys for expired data as well
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetKeys()
        {
            return _cacheDictionary.Keys;
        }

        public T GetOrCreate<T>(string key, Func<T> createFn, DateTimeOffset? expiry = null)
        {
            if (!TryGetValue<T>(key, out var value))
            {
                value = createFn();
                return Set<T>(key, value, expiry);
            }
            return value;
        }

        public T Set<T>(string key, T value, DateTimeOffset? expiry = null)
        {
            var fixedExpiry = expiry ?? DateTimeOffset.MaxValue;

            // If over capacity, scan once and remove expired or arbitrary
            if (_cacheDictionary.Count >= _config.SizeLimit)
            {
                foreach (var kv in _cacheDictionary)
                {
                    if (_cacheDictionary.Count < _config.SizeLimit)
                        break;

                    if (kv.Value.expiry < DateTimeOffset.UtcNow)
                    {
                        _cacheDictionary.TryRemove(kv.Key, out _);
                    }
                }
            }

            // If still at capacity, remove arbitrary entries
            while (_cacheDictionary.Count > _config.SizeLimit)
            {
                var keyToRemove = _cacheDictionary.Keys.FirstOrDefault();
                if (keyToRemove == null)
                    break;

                _cacheDictionary.TryRemove(keyToRemove, out _);
            }

            _cacheDictionary.AddOrUpdate(key, (value, fixedExpiry), (k, v) => (value, fixedExpiry));
            return value;
        }

        public void Remove(string key)
        {
            _cacheDictionary.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cacheDictionary.Clear();
        }
    }
}
