using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public class RedisCache : CacheBase, ICache
    {
        private Microsoft.Extensions.Caching.Redis.RedisCache mRedisCache;

        public RedisCache(dynamic options)
        {
            if (options != null)
            {
                StringBuilder sbConfig = new StringBuilder();
                if (options.Has("Host"))
                {
                    if (!string.IsNullOrWhiteSpace("" + options.Host))
                    {
                        sbConfig.Append(options.Host);
                    }
                    else
                    {
                        throw new Exception("RedisCache选项Host设置不能为空。");
                    }
                }
                else
                {
                    throw new Exception("RedisCache选项Host必须设置。");
                }

                if (!options.Has("Port"))
                {
                    options.Port = 6379;
                }
                if (!string.IsNullOrWhiteSpace("" + options.Port))
                {
                    int port;
                    if (int.TryParse("" + options.Port, out port))
                    {
                        sbConfig.Append(string.Concat(":", port));
                    }
                    else
                    {
                        throw new InvalidCastException("RedisCache选项Port类型必须为整型数字。");
                    }
                }
                else
                {
                    throw new Exception("RedisCache选项Port设置不能为空。");
                }

                if (options.Has("Retry"))
                {
                    sbConfig.Append(string.Concat(",connectRetry=", options.Retry));
                }

                if (options.Has("Timeout"))
                {
                    sbConfig.Append(string.Concat(",connectTimeout=", options.Timeout));
                }

                if (options.Has("Database"))
                {
                    sbConfig.Append(string.Concat(",defaultDatabase=", options.Database));
                }

                if (options.Has("Password"))
                {
                    sbConfig.Append(string.Concat(",password=", options.Password));
                }

                if (options.Has("Ssl"))
                {
                    bool ssl;
                    if (bool.TryParse("" + options.Ssl, out ssl))
                    {
                        if (ssl)
                        {
                            sbConfig.Append(",ssl=true");
                            if (options.Has("SslHost"))
                            {
                                sbConfig.Append(string.Concat(",sslHost=", options.SslHost));
                            }
                            if (options.Has("SslProtocols"))
                            {
                                sbConfig.Append(string.Concat(",sslProtocols=", options.SslProtocols));
                            }
                        }
                    }
                }

                Microsoft.Extensions.Caching.Redis.RedisCacheOptions rcp =
                    new Microsoft.Extensions.Caching.Redis.RedisCacheOptions();
                rcp.Configuration = sbConfig.ToString();
                if (options.Has("InstanceName"))
                {
                    rcp.InstanceName = options.InstanceName;
                }

                mRedisCache = new Microsoft.Extensions.Caching.Redis.RedisCache(rcp);
                try
                {
                    mRedisCache.GetString("_$test$_");
                }
                catch (Exception exp)
                {
                    throw new Exception(string.Concat("Redis不能连接：", options.Host, ":", options.Port), exp);
                }
            }
            else
            {
                throw new ArgumentNullException("options");
            }
        }

        private DistributedCacheEntryOptions CreateCacheOptions(
            long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
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
            mRedisCache.Set(key, value);
        }

        public override void Set(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.Set(key, value, options);
        }

        public override async Task SetAsync(string key, byte[] value)
        {
            await mRedisCache.SetAsync(key, value);
        }

        public override async Task SetAsync(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetAsync(key, value, options);
        }

        public override byte[] Get(string key)
        {
            return mRedisCache.Get(key);
        }

        public override async Task<byte[]> GetAsync(string key)
        {
            return await mRedisCache.GetAsync(key);
        }

        public override void SetBoolean(string key, bool value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public override void SetBoolean(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public override async Task SetBooleanAsync(string key, bool value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public override async Task SetBooleanAsync(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public override bool GetBoolean(string key)
        {
            bool bRet;
            string value = mRedisCache.GetString(key);
            if (bool.TryParse(value, out bRet))
            {
                return bRet;
            }
            return false;   
        }

        public override async Task<bool> GetBooleanAsync(string key)
        {
            bool bRet;
            string value = await mRedisCache.GetStringAsync(key);
            if (bool.TryParse(value, out bRet))
            {
                return bRet;
            }
            return false;
        }

        public override void SetInt32(string key, int value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public override void SetInt32(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public override async Task SetInt32Async(string key, int value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public override async Task SetInt32Async(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public override int? GetInt32(string key)
        {
            string value = mRedisCache.GetString(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                int iRet;
                if (int.TryParse(value, out iRet))
                {
                    return iRet;
                }
            }
            return null;
        }

        public override async Task<int?> GetInt32Async(string key)
        {
            string value = await mRedisCache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                int iRet;
                if (int.TryParse(value, out iRet))
                {
                    return iRet;
                }
            }
            return null;
        }

        public override void SetInt64(string key, long value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public override async Task SetInt64Async(string key, long value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public override void SetInt64(string key, long value, long seconds, ExpirationType type)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public override async Task SetInt64Async(string key, long value, long seconds, ExpirationType type)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public override long? GetInt64(string key)
        {
            string value = mRedisCache.GetString(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                long lRet;
                if (long.TryParse(value, out lRet))
                {
                    return lRet;
                }
            }
            return null;
        }

        public override async Task<long?> GetInt64Async(string key)
        {
            string value = await mRedisCache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                long lRet;
                if (long.TryParse(value, out lRet))
                {
                    return lRet;
                }
            }
            return null;
        }

        public override void SetDouble(string key, double value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public override async Task SetDoubleAsync(string key, double value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public override void SetDouble(string key, double value, long seconds, ExpirationType type)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public override async Task SetDoubleAsync(string key, double value, long seconds, ExpirationType type)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public override double? GetDouble(string key)
        {
            string value = mRedisCache.GetString(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                double dRet;
                if (double.TryParse(value, out dRet))
                {
                    return dRet;
                }
            }
            return null;
        }

        public override async Task<double?> GetDoubleAsync(string key)
        {
            string value = await mRedisCache.GetStringAsync(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                double dRet;
                if (double.TryParse(value, out dRet))
                {
                    return dRet;
                }
            }
            return null;
        }

        public override void SetString(string key, string value)
        {
            mRedisCache.SetString(key, value);
        }

        public override void SetString(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value, options);
        }

        public override async Task SetStringAsync(string key, string value)
        {
            await mRedisCache.SetStringAsync(key, value);
        }

        public override async Task SetStringAsync(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value, options);
        }

        public override string GetString(string key)
        {
            return mRedisCache.GetString(key);
        }

        public override async Task<string> GetStringAsync(string key)
        {
            return await mRedisCache.GetStringAsync(key);
        }

        public override bool ContainsKey(string key)
        {
            byte[] value = mRedisCache.Get(key);
            return value != null;
        }

        public override bool TryGetValue<T>(string key, out T result)
        {
            result = default(T);
            if (ContainsKey(key.ToString()))
            {
                string value = mRedisCache.GetString(key);
                object typValue = Convert.ChangeType(value, typeof(T));
                result = (T)typValue;
                return true;
            }
            return false;
        }

        public override void Remove(string key)
        {
            mRedisCache.Remove(key);
        }

        public override async Task RemoveAsync(string key)
        {
            await mRedisCache.RemoveAsync(key);
        }
    }
}
