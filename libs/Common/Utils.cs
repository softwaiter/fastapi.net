using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace CodeM.FastApi.Common
{
    public static class Utils
    {
        private static bool sIsDevelopmentChecked = false;
        private static bool sIsDevelopment = false;
        public static bool IsDev()
        {
            if (!sIsDevelopmentChecked)
            {
                string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                sIsDevelopment = "Development".Equals(env, StringComparison.OrdinalIgnoreCase);
                sIsDevelopmentChecked = true;
            }
            return sIsDevelopment;
        }

        private static bool sIsProductionChecked = false;
        private static bool sIsProduction = false;
        public static bool IsProd()
        {
            if (!sIsProductionChecked)
            {
                string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                sIsProduction = "Production".Equals(env, StringComparison.OrdinalIgnoreCase);
                sIsProductionChecked = true;
            }
            return sIsProduction;
        }

        private static ConcurrentDictionary<string, bool> sEnvs = new ConcurrentDictionary<string, bool>();
        public static bool IsEnv(string envName)
        {
            bool bRet = false;
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string key = env != null ? env.ToLower() : "";
            if (!sEnvs.TryGetValue(key, out bRet))
            {
                bRet = key.Equals(envName, StringComparison.OrdinalIgnoreCase);
                sEnvs.TryAdd(key, bRet);
            }
            return bRet;
        }

        private static Dictionary<string, bool> sTypeMethods = new Dictionary<string, bool>();
        public static bool IsMethodExists(Type _typ, string method)
        {
            string key = string.Concat(_typ.FullName, "`", method);
            if (!sTypeMethods.ContainsKey(key))
            {
                MethodInfo mi = _typ.GetMethod(method,
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.Static | BindingFlags.IgnoreCase);
                sTypeMethods[key] = mi != null;
            }
            return sTypeMethods[key];
        }

        public static bool IsMethodExists(object obj, string method)
        {
            Type _typ = obj.GetType();
            return IsMethodExists(_typ, method);
        }
    }
}
