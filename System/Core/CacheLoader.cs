using CodeM.Common.Ioc;
using CodeM.Common.Tools.Json;
using CodeM.FastApi.Cache;
using CodeM.FastApi.Config;
using System;

namespace CodeM.FastApi.System.Core
{
    internal class CacheLoader
    {
        public static void Load(ApplicationConfig config)
        {
            if (config.Settings.Has("Cache"))
            {
                if (config.Settings.Cache is DynamicObjectExt)
                {
                    dynamic cacheSettings = config.Settings.Cache;
                    foreach (string key in cacheSettings.Keys)
                    {
                        dynamic cacheItem = cacheSettings[key];
                        if (!cacheItem.Has("Type"))
                        {
                            throw new Exception("Cache配置缺少Type属性。");
                        }

                        bool isDefault = false;
                        if (cacheItem.Has("Default"))
                        {
                            isDefault = cacheItem.Default;
                        }

                        string cacheClassName = string.Concat("CodeM.FastApi.Cache.", cacheItem.Type, "Cache");
                        dynamic options = cacheItem["Options"];
                        ICache cacheInst = IocUtils.GetObject<ICache>(cacheClassName, new object[] { options });
                        CacheManager.Add(key, cacheInst, isDefault);
                    }
                }
            }
        }
    }
}
