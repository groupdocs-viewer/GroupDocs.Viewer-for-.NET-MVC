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
            _cache = cache;
            _keyLockerStore = keyLockerStore;
        }

        public void Set(string key, object value)
        {
            lock (_keyLockerStore.GetLockerFor(key))
            {
                _cache.Set(key, value);
            }
        }

        public bool TryGetValue<TEntry>(string key, out TEntry value)
        {
            lock (_keyLockerStore.GetLockerFor(key))
            {
                return _cache.TryGetValue(key, out value);
            }
        }

        public IEnumerable<string> GetKeys(string filter)
        {
            lock (_keyLockerStore.GetLockerFor("get_keys"))
            {
                return _cache.GetKeys(filter);
            }
        }
    }
}