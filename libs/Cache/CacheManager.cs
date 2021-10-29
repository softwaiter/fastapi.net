using System.Collections.Concurrent;

namespace CodeM.FastApi.Cache
{
    public class CacheManager
    {
        private static ConcurrentDictionary<string, ICache> sCaches = new ConcurrentDictionary<string, ICache>();
        private static string sDefaultCacheName = null;

        public static void Add(string cacheName, ICache cache, bool isDefault = false)
        {
            sCaches.AddOrUpdate(cacheName, cache, (key, value) =>
            {
                return cache;
            });

            if (isDefault)
            {
                sDefaultCacheName = cacheName;
            }
        }

        public static ICache Remove(string cacheName)
        {
            ICache result;
            if (sCaches.TryRemove(cacheName, out result))
            {
                return result;
            }
            return null;
        }

        public static void Clear()
        {
            sCaches.Clear();
        }

        public static ICache Cache(string cacheName = null)
        {
            if (sCaches.Count > 0)
            {
                if (cacheName == null)
                {
                    cacheName = sDefaultCacheName;
                }

                ICache result;
                if (sCaches.TryGetValue(cacheName, out result))
                {
                    return result;
                }
            }
            return null;
        }
    }
}
