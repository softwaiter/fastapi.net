using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public class LocalCache : ICache
    {
        private MemoryCache mLocalCache;

        public LocalCache()
        {
            MemoryCacheOptions options = new MemoryCacheOptions();
            mLocalCache = new MemoryCache(options);
        }

        private MemoryCacheEntryOptions CreateCacheOptions(long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
            switch (type)
            {
                case ExpirationType.Absolute:
                    options.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(seconds);
                    break;
                case ExpirationType.RelativeToNow:
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
                    break;
                case ExpirationType.Sliding:
                    options.SlidingExpiration = TimeSpan.FromSeconds(seconds);
                    break;
            }
            return options;
        }

        public void Set(string key, byte[] value)
        {
            mLocalCache.Set<byte[]>(key, value);
        }

        public async Task SetAsync(string key, byte[] value)
        {
            await Task.Run(() =>
            {
                Set(key, value);
            });
        }

        public void Set(string key, byte[] value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<byte[]>(key, value, op);
        }

        public async Task SetAsync(string key, byte[] value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                Set(key, value, seconds, type);
            });
        }


        public byte[] Get(string key)
        {
            return mLocalCache.Get<byte[]>(key);
        }

        public async Task<byte[]> GetAsync(string key)
        {
            return await Task.Run(() =>
            {
                return Get(key);
            });
        }

        public void SetBoolean(string key, bool value)
        {
            mLocalCache.Set<bool>(key, value);
        }

        public async Task SetBooleanAsync(string key, bool value)
        {
            await Task.Run(() =>
            {
                SetBoolean(key, value);
            });
        }

        public void SetBoolean(string key, bool value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<bool>(key, value, op);
        }

        public async Task SetBooleanAsync(string key, bool value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetBoolean(key, value, seconds, type);
            });
        }


        public bool GetBoolean(string key)
        {
            return mLocalCache.Get<bool>(key);
        }

        public async Task<bool> GetBooleanAsync(string key)
        {
            return await Task.Run(() =>
            {
                return GetBoolean(key);
            });
        }

        public void SetInt32(string key, int value)
        {
            mLocalCache.Set<Int32>(key, value);
        }

        public async Task SetInt32Async(string key, int value)
        {
            await Task.Run(() =>
            {
                SetInt32(key, value);
            });
        }

        public void SetInt32(string key, int value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<int>(key, value, op);
        }

        public async Task SetInt32Async(string key, int value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetInt32(key, value, seconds, type);
            });
        }

        public int? GetInt32(string key)
        {
            return mLocalCache.Get<Int32>(key);
        }

        public async Task<int?> GetInt32Async(string key)
        {
            return await Task.Run(() =>
            {
                return GetInt32(key);
            });
        }

        public void SetString(string key, string value)
        {
            mLocalCache.Set<string>(key, value);
        }

        public async Task SetStringAsync(string key, string value)
        {
            await Task.Run(() =>
            {
                SetString(key, value);
            });
        }

        public void SetString(string key, string value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<string>(key, value, op);
        }

        public async Task SetStringAsync(string key, string value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetString(key, value, seconds, type);
            });
        }

        public string GetString(string key)
        {
            return mLocalCache.Get<string>(key);
        }

        public async Task<string> GetStringAsync(string key)
        {
            return await Task.Run(() =>
            {
                return GetString(key);
            });
        }

        public bool TryGetValue(object key, out object result)
        {
            return mLocalCache.TryGetValue(key, out result);
        }

        public void Remove(string key)
        {
            mLocalCache.Remove(key);
        }

        public async Task RemoveAsync(string key)
        {
            await Task.Run(() =>
            {
                Remove(key);
            });
        }
    }
}
