// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HDDL.Collections
{
    /// <summary>
    /// Implements a caching system where items can expire and will automatically be disposed of when they do
    /// </summary>
    /// <typeparam name="ContentType">The type being cached</typeparam>
    /// <typeparam name="KeyType">The key type used to refer to it</typeparam>
    class ExpiringCache<KeyType, ContentType> : IDisposable
    {
        /// <summary>
        /// The actual cache
        /// </summary>
        private ConcurrentDictionary<KeyType, ContentType> _cache;

        /// <summary>
        /// Tracks when each key was last accessed
        /// </summary>
        private ConcurrentDictionary<KeyType, DateTime> _lastAccessed;

        /// <summary>
        /// Task that handles disposal of stale items
        /// </summary>
        private Task _staleMonitorTask;

        /// <summary>
        /// Whether or not the cache has been disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Duration (in milliseconds) until an unaccessed item goes stale
        /// </summary>
        public double StalePeriod { get; private set; }

        /// <summary>
        /// Performs the same actions as a Get or Set explicit call
        /// </summary>
        /// <param name="key">The key to retrieve or add / update</param>
        /// <returns></returns>
        public ContentType this[KeyType key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Set(key, value);
            }
        }

        /// <summary>
        /// Creates an expiring cache instance
        /// </summary>
        /// <param name="stalePeriod">The amount of milliseconds before the item becomes stale and is removed (negative number means never go stale)</param>
        public ExpiringCache(double stalePeriod)
        {
            _cache = new ConcurrentDictionary<KeyType, ContentType>();
            _lastAccessed = new ConcurrentDictionary<KeyType, DateTime>();
            StalePeriod = stalePeriod;
            _disposed = false;

            if (StalePeriod > 0)
            {
                _staleMonitorTask = Task.Run(() =>
                {
                    var stale = new List<KeyType>();
                    var sleepDuration = 100; // milliseconds
                    while (!_disposed)
                    {
                        stale.Clear();
                        foreach (var key in _lastAccessed.Keys)
                        {
                            if (_lastAccessed.TryGetValue(key, out DateTime lastUsed))
                            {
                                if (DateTime.Now.Subtract(lastUsed).TotalMilliseconds >= StalePeriod)
                                {
                                    stale.Add(key);
                                }
                            }
                        }

                        if (stale.Count > 0)
                        {
                            stale.ForEach(key => Toss(key));
                        }
                        else
                        {
                            sleepDuration *= 2;
                            if (sleepDuration > 1000) sleepDuration = 1000;
                        }

                        if (!_disposed)
                        {
                            Task.Delay(sleepDuration).Wait();
                        }
                    }
                });
            }
        }

        #region Management Methods

        /// <summary>
        /// Adds or updates the given item in the cache
        /// </summary>
        /// <param name="key">The key to reference it by</param>
        /// <param name="value">The value to referernce</param>
        public void Set(KeyType key, ContentType value)
        {
            _lastAccessed[key] = DateTime.Now;
            _cache[key] = value;
        }

        /// <summary>
        /// Retrieves and returns the contents of the given key
        /// </summary>
        /// <param name="key">The key to retrieve</param>
        /// <returns></returns>
        public ContentType Get(KeyType key)
        {
            if (_cache.TryGetValue(key, out ContentType result))
            {
                _lastAccessed[key] = DateTime.Now;
                return result;
            }

            return default(ContentType);
        }

        /// <summary>
        /// Checks to see if the given key exists within the cache
        /// </summary>
        /// <param name="key">The key to check for</param>
        /// <returns></returns>
        public bool Has(KeyType key)
        {
            if (_cache.ContainsKey(key))
            {
                _lastAccessed[key] = DateTime.Now;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Discards the given key and its content
        /// </summary>
        /// <param name="key">The key to discard</param>
        public ContentType Toss(KeyType key)
        {
            if (Has(key))
            {
                if (_cache.TryRemove(key, out ContentType content) &&
                    _lastAccessed.TryRemove(key, out DateTime dt))
                {
                    return content;
                }
            }

            return default(ContentType);
        }

        #endregion

        public void Dispose()
        {
            _disposed = true;
            _staleMonitorTask = null;
        }
    }
}
