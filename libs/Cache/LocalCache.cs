using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public class LocalCache : CacheBase
    {
        private MemoryCache mLocalCache;

        public LocalCache(dynamic options)
        {
            MemoryCacheOptions mco = new MemoryCacheOptions();
            mLocalCache = new MemoryCache(mco);
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

        public override void Set(string key, byte[] value)
        {
            mLocalCache.Set<byte[]>(key, value);
        }

        public override async Task SetAsync(string key, byte[] value)
        {
            await Task.Run(() =>
            {
                Set(key, value);
            });
        }

        public override void Set(string key, byte[] value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<byte[]>(key, value, op);
        }

        public override async Task SetAsync(string key, byte[] value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                Set(key, value, seconds, type);
            });
        }

        public override byte[] Get(string key)
        {
            byte[] buff = mLocalCache.Get<byte[]>(key);
            return buff;
        }

        public override async Task<byte[]> GetAsync(string key)
        {
            return await Task.Run(() =>
            {
                return Get(key);
            });
        }

        public override void SetBoolean(string key, bool value)
        {
            mLocalCache.Set<bool>(key, value);
        }

        public override async Task SetBooleanAsync(string key, bool value)
        {
            await Task.Run(() =>
            {
                SetBoolean(key, value);
            });
        }

        public override void SetBoolean(string key, bool value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<bool>(key, value, op);
        }

        public override async Task SetBooleanAsync(string key, bool value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetBoolean(key, value, seconds, type);
            });
        }

        public override bool GetBoolean(string key)
        {
            return mLocalCache.Get<bool>(key);
        }

        public override async Task<bool> GetBooleanAsync(string key)
        {
            return await Task.Run(() =>
            {
                return GetBoolean(key);
            });
        }

        public override void SetInt32(string key, int value)
        {
            mLocalCache.Set<Int32>(key, value);
        }

        public override async Task SetInt32Async(string key, int value)
        {
            await Task.Run(() =>
            {
                SetInt32(key, value);
            });
        }

        public override void SetInt32(string key, int value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<int>(key, value, op);
        }

        public override async Task SetInt32Async(string key, int value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetInt32(key, value, seconds, type);
            });
        }

        public override int? GetInt32(string key)
        {
            return mLocalCache.Get<int?>(key);
        }

        public override async Task<int?> GetInt32Async(string key)
        {
            return await Task.Run(() =>
            {
                return GetInt32(key);
            });
        }

        public override void SetInt64(string key, long value)
        {
            mLocalCache.Set<Int64>(key, value);
        }

        public override async Task SetInt64Async(string key, long value)
        {
            await Task.Run(() =>
            {
                SetInt64(key, value);
            });
        }

        public override void SetInt64(string key, long value, long seconds, ExpirationType type)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<Int64>(key, value, op);
        }

        public override async Task SetInt64Async(string key, long value, long seconds, ExpirationType type)
        {
            await Task.Run(() =>
            {
                SetInt64(key, value, seconds, type);
            });
        }

        public override long? GetInt64(string key)
        {
            return mLocalCache.Get<long?>(key);
        }

        public override async Task<long?> GetInt64Async(string key)
        {
            return await Task.Run(() =>
            {
                return GetInt64(key);
            });
        }

        public override void SetDouble(string key, double value)
        {
            mLocalCache.Set<double>(key, value);
        }

        public override async Task SetDoubleAsync(string key, double value)
        {
            await Task.Run(() =>
            {
                SetDouble(key, value);
            });
        }

        public override void SetDouble(string key, double value, long seconds, ExpirationType type)
        {
            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<double>(key, value, op);
        }

        public override async Task SetDoubleAsync(string key, double value, long seconds, ExpirationType type)
        {
            await Task.Run(() =>
            {
                SetDouble(key, value, seconds, type);
            });
        }

        public override double? GetDouble(string key)
        {
            return mLocalCache.Get<double?>(key);
        }

        public override async Task<double?> GetDoubleAsync(string key)
        {
            return await Task.Run(() =>
            {
                return GetDouble(key);
            });
        }

        public override void SetString(string key, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null");
            }
            mLocalCache.Set<string>(key, value);
        }

        public override async Task SetStringAsync(string key, string value)
        {
            await Task.Run(() =>
            {
                SetString(key, value);
            });
        }

        public override void SetString(string key, string value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Value cannot be null");
            }

            MemoryCacheEntryOptions op = CreateCacheOptions(seconds, type);
            mLocalCache.Set<string>(key, value, op);
        }

        public override async Task SetStringAsync(string key, string value,
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            await Task.Run(() =>
            {
                SetString(key, value, seconds, type);
            });
        }

        public override string GetString(string key)
        {
            return mLocalCache.Get<string>(key);
        }

        public override async Task<string> GetStringAsync(string key)
        {
            return await Task.Run(() =>
            {
                return GetString(key);
            });
        }

        public override bool ContainsKey(string key)
        {
            object value;
            return TryGetValue(key, out value);
        }

        public override bool TryGetValue<T>(string key, out T result)
        {
            result = default(T);
            object value;
            if (mLocalCache.TryGetValue(key, out value))
            {
                result = (T)value;
                return true;
            }
            return false;
        }

        public override void Remove(string key)
        {
            mLocalCache.Remove(key);
        }

        public override async Task RemoveAsync(string key)
        {
            await Task.Run(() =>
            {
                Remove(key);
            });
        }
    }
}
