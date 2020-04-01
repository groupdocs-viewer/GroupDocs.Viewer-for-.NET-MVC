using System.Collections.Concurrent;

namespace GroupDocs.Viewer.MVC.Products.Viewer.Cache
{
    public class ConcurrentDictionaryKeyLockerStore : IKeyLockerStore
    {
        private readonly ConcurrentDictionary<string, object> _keyLockerMap;
        private readonly string _uniqueKeyPrefix;

        public ConcurrentDictionaryKeyLockerStore(ConcurrentDictionary<string, object> keyLockerMap, string uniqueKeyPrefix)
        {
            _keyLockerMap = keyLockerMap;
            _uniqueKeyPrefix = uniqueKeyPrefix;
        }

        public object GetLockerFor(string key)
        {
            string uniqueKey = GetUniqueKey(key);
            return _keyLockerMap.GetOrAdd(uniqueKey, k => new object());
        }

        private string GetUniqueKey(string key)
        {
            return $"{_uniqueKeyPrefix}_{key}";
        }
    }
}