using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeM.FastApi.Common
{
    public static class Utils
    {
        private static bool sIsDevelopmentChecked = false;
        private static bool sIsDevelopment = false;
        public static bool IsDevelopment()
        {
            if (!sIsDevelopmentChecked)
            {
                string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                sIsDevelopment = "Development".Equals(env, StringComparison.OrdinalIgnoreCase);
                sIsDevelopmentChecked = true;
            }
            return sIsDevelopment;
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
