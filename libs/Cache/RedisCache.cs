using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CodeM.FastApi.Cache
{
    public class RedisCache : ICache
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

        public void Set(string key, byte[] value)
        {
            mRedisCache.Set(key, value);
        }

        public void Set(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.Set(key, value, options);
        }

        public async Task SetAsync(string key, byte[] value)
        {
            await mRedisCache.SetAsync(key, value);
        }

        public async Task SetAsync(string key, byte[] value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetAsync(key, value, options);
        }

        public byte[] Get(string key)
        {
            return mRedisCache.Get(key);
        }

        public async Task<byte[]> GetAsync(string key)
        {
            return await mRedisCache.GetAsync(key);
        }

        public void SetBoolean(string key, bool value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public void SetBoolean(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public async Task SetBooleanAsync(string key, bool value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public async Task SetBooleanAsync(string key, bool value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public bool GetBoolean(string key)
        {
            bool bRet;
            string value = mRedisCache.GetString(key);
            if (bool.TryParse(value, out bRet))
            {
                return bRet;
            }
            return false;   
        }

        public async Task<bool> GetBooleanAsync(string key)
        {
            bool bRet;
            string value = await mRedisCache.GetStringAsync(key);
            if (bool.TryParse(value, out bRet))
            {
                return bRet;
            }
            return false;
        }

        public void SetInt32(string key, int value)
        {
            mRedisCache.SetString(key, value.ToString());
        }

        public void SetInt32(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value.ToString(), options);
        }

        public async Task SetInt32Async(string key, int value)
        {
            await mRedisCache.SetStringAsync(key, value.ToString());
        }

        public async Task SetInt32Async(string key, int value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value.ToString(), options);
        }

        public int? GetInt32(string key)
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

        public async Task<int?> GetInt32Async(string key)
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

        public void SetString(string key, string value)
        {
            mRedisCache.SetString(key, value);
        }

        public void SetString(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            mRedisCache.SetString(key, value, options);
        }

        public async Task SetStringAsync(string key, string value)
        {
            await mRedisCache.SetStringAsync(key, value);
        }

        public async Task SetStringAsync(string key, string value, long seconds, ExpirationType type = ExpirationType.RelativeToNow)
        {
            DistributedCacheEntryOptions options = CreateCacheOptions(seconds, type);
            await mRedisCache.SetStringAsync(key, value, options);
        }

        public string GetString(string key)
        {
            return mRedisCache.GetString(key);
        }

        public async Task<string> GetStringAsync(string key)
        {
            return await mRedisCache.GetStringAsync(key);
        }

        public bool TryGetValue(object key, out object result)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(string key)
        {
            mRedisCache.Remove(key);
        }

        public async Task RemoveAsync(string key)
        {
            await mRedisCache.RemoveAsync(key);
        }
    }
}
