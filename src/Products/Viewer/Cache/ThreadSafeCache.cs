using GroupDocs.Viewer.Caching;
using System.Collections.Generic;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    class ThreadSafeCache : ICache
    {
        private readonly ICache _cache;
        private readonly IKeyLockerStore _keyLockerStore;

        public ThreadSafeCache(ICache cache, IKeyLockerStore keyLockerStore)
        {
            this._cache = cache;
            this._keyLockerStore = keyLockerStore;
        }

        public void Set(string key, object value)
        {
            lock (this._keyLockerStore.GetLockerFor(key))
            {
                this._cache.Set(key, value);
            }
        }

        public bool TryGetValue<TEntry>(string key, out TEntry value)
        {
            lock (this._keyLockerStore.GetLockerFor(key))
            {
                return this._cache.TryGetValue(key, out value);
            }
        }

        public IEnumerable<string> GetKeys(string filter)
        {
            lock (this._keyLockerStore.GetLockerFor("get_keys"))
            {
                return this._cache.GetKeys(filter);
            }
        }
    }
}