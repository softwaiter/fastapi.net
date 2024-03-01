using CodeM.FastApi.Config.Settings;
using CodeM.FastApi.System.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;

namespace CodeM.FastApi.System.Utils
{
    public class CorsUtils
    {
        public static string GetAllowOrigin()
        {
            CorsSetting corsSetting = Application.Instance().Config().Cors;

            string allowOrigin = "*";
            if (corsSetting.Options.AllowSites != null &&
                !corsSetting.Options.AllowSites.Contains<string>("*"))
            {
                StringValues origins;
                if (CurrentContext.Context.Request.Headers.TryGetValue("Origin", out origins))
                {
                    foreach (string item in origins)
                    {
                        if (corsSetting.Options.AllowSites.Contains<string>(item,
                            StringComparer.OrdinalIgnoreCase))
                        {
                            allowOrigin = item;
                            break;
                        }
                    }
                }
                else
                {
                    return corsSetting.Options.AllowSites[0];
                }
            }
            return allowOrigin;
        }

        public static string GetAllowMethods()
        {
            CorsSetting corsSetting = Application.Instance().Config().Cors;

            string allowMethods = "*";
            if (corsSetting.Options.AllowMethods != null)
            {
                allowMethods = string.Join(",", corsSetting.Options.AllowMethods).ToUpper();
            }
            if (allowMethods.Contains("*"))
            {
                allowMethods = "GET,POST,HEAD,PATCH,PUT,DELETE,OPTIONS,TRACE,CONNECT";
            }
            return allowMethods;
        }

        public static string GetAllowHeaders()
        {
            return CurrentContext.Context.Request.Headers["Access-Control-Request-Headers"];
        }

        public static string GetAllowCredentials()
        {
            CorsSetting corsSetting = Application.Instance().Config().Cors;
            return corsSetting.Options.SupportsCredentials ? "true" : "false";
        }

        public static void SetCorsHeaders(HttpContext context)
        {
            SetCorsHeaders(context.Response);
        }

        public static void SetCorsHeaders(HttpResponse response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", GetAllowOrigin());
            response.Headers.Add("Access-Control-Allow-Methods", GetAllowMethods());
            response.Headers.Add("Access-Control-Allow-Headers", GetAllowHeaders());
            response.Headers.Add("Access-Control-Allow-Credentials", GetAllowCredentials());
        }
    }
}
