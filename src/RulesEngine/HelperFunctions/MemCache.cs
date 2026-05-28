// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RulesEngine.HelperFunctions
{
    internal class MemCache
    {
        //use a simple in-memory cache with expiry and size limit. Eviction is based on expiry time (oldest first)
        public int SizeLimit { get; set; } = 1000;
        private ConcurrentDictionary<string, (object value, DateTimeOffset expiry)> _cacheDictionary;

        public MemCache(int sl)
        {
            SizeLimit = sl;
            _cacheDictionary = new ConcurrentDictionary<string, (object value, DateTimeOffset expiry)>();
        }

        public bool TryGetValue<T>(string key,out T value)
        {
            value = default;
            if (_cacheDictionary.TryGetValue(key, out var cacheItem))
            {
                if(cacheItem.expiry < DateTimeOffset.UtcNow)
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
            if(!TryGetValue<T>(key,out var value))
            {
                value = createFn();
                return Set<T>(key,value,expiry);
            }
            return value;
        }

        public T Set<T>(string key, T value, DateTimeOffset? expiry = null)
        {
            var fixedExpiry = expiry ?? DateTimeOffset.MaxValue;

            // If at capacity, evict oldest by expiry
            while (_cacheDictionary.Count > SizeLimit)
            {
                var oldest = _cacheDictionary.OrderBy(kv => kv.Value.expiry).FirstOrDefault();
                if (oldest.Key != null)
                {
                    _cacheDictionary.TryRemove(oldest.Key, out _);
                }
                else
                {
                    break; // Shouldn't happen but prevents infinite loop
                }
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
