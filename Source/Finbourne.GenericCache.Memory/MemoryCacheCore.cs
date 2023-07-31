using Finbourne.GenericCache.Core.Interface;
using Finbourne.GenericCache.Core.Model.Enum;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finbourne.GenericCache.Memory
{
    public class MemoryCacheCore : IGenericCache
    {
        private readonly MemoryCacheConfig config;
        private readonly ILogger<MemoryCacheCore> logger;
        private readonly ConcurrentDictionary<string, object> store;
        private readonly LinkedList<string> queue;
        private readonly ConcurrentDictionary<string, Action<string, CacheDeletionReasonEnum, object>> subscriptionsStore;
        private readonly object _lock = new object();


        public MemoryCacheCore(MemoryCacheConfig config, ILogger<MemoryCacheCore> logger)
        {
            if (config.MaxSize <= 0)
            {
                throw new ArgumentException("MaxSize of cache must be greater than 0.", nameof(config.MaxSize));
            }
            this.config = config;
            this.logger = logger;
            this.store = new ConcurrentDictionary<string, object>();
            this.queue = new LinkedList<string>();
            this.subscriptionsStore = new ConcurrentDictionary<string, Action<string, CacheDeletionReasonEnum, object>>();
        }

        public MemoryCacheCore(MemoryCacheConfig config) : this(config, null)
        {
        }

        public MemoryCacheCore() : this(new MemoryCacheConfig() { MaxSize = 100 }, null)
        {
        }

        public Task DeleteAsync<T>(string key)
        {
            if (store.TryRemove(key, out var removedValue))
            {
                foreach (var subscription in subscriptionsStore)
                {
                    subscription.Value(key, CacheDeletionReasonEnum.ManualDelete, removedValue);
                }
                logger?.LogInformation($"Key {key} removed from cache.");
            }
            else
            {
                logger?.LogWarning($"Key {key} not found in cache.");
            }
            return Task.CompletedTask;
        }

        public Task<bool> TryGetAsync<T>(string key, out T value)
        {
            if (store.TryGetValue(key, out var _value))
            {
                logger?.LogInformation($"Key {key} found in cache.");
                value = (T)_value;
                // Now we make sure we keep track of what was accessed last
                lock (_lock)
                {
                    queue.Remove(key);
                    queue.AddFirst(key);
                }
                return Task.FromResult(true);
            }
            logger?.LogInformation($"Key {key} not found in cache.");
            value = default;
            return Task.FromResult(false);
        }

        public Task PurgeAsync()
        {
            logger?.LogInformation($"Purging cache.");
            foreach (var value in store)
            {
                foreach (var subscription in subscriptionsStore)
                {
                    subscription.Value(value.Key, CacheDeletionReasonEnum.Purge, value.Value);
                }
            }
            store.Clear();
            queue.Clear();
            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T value)
        {
            lock (_lock)
            {
                if (store.Count >= config.MaxSize && !store.ContainsKey(key))
                {
                    logger?.LogInformation($"Cache capacity reached. Removing oldest key.");
                    var oldestKey = queue.Last?.Value;
                    if (!string.IsNullOrEmpty(oldestKey))
                    {
                        store.TryRemove(oldestKey, out var oldValue);
                        logger?.LogInformation($"Oldest key {oldestKey} removed from cache.");
                        queue.RemoveLast();
                        foreach (var subscription in subscriptionsStore)
                        {
                            subscription.Value(oldestKey, CacheDeletionReasonEnum.CapacityReached, oldValue);
                        }
                    }
                }
                store.AddOrUpdate(key, value, (_, _) => value);
                // We might consider not adding this here because technically it was not used.
                // But it seems like a cheap price for simplicity
                queue.AddFirst(key);
            }

            logger?.LogInformation($"Key {key} added to cache.");
            return Task.CompletedTask;
        }

        public Task UnSubscribeDeleteAsync<T>(string subscription)
        {
            logger?.LogInformation($"Unsubscribing from id {subscription}.");
            subscriptionsStore.TryRemove(subscription, out _);
            return Task.CompletedTask;
        }

        public Task<string> SubscribeDeleteAsync<T>(Action<string, CacheDeletionReasonEnum, T> action)
        {
            var guid = Guid.NewGuid().ToString();
            var wrappedAction = new Action<string, CacheDeletionReasonEnum, object>((k, r, v) => action(k, r, (T)v));
            subscriptionsStore.AddOrUpdate(guid, wrappedAction, (k, v) => wrappedAction);
            logger?.LogInformation($"Subscribed to deletion with subscription id: {guid}.");
            return Task.FromResult(guid);
        }
    }
}
