using CodeM.FastApi.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CodeM.FastApi.System.Middlewares
{
    public class CorsMiddleware
    {
        private readonly RequestDelegate _next;
        private ApplicationConfig mConfig;

        public CorsMiddleware(RequestDelegate next, ApplicationConfig config)
        {
            _next = next;
            mConfig = config;
        }

        public Task Invoke(HttpContext context)
        {
            if (mConfig.Cors.Options.AllowSites != null &&
                !mConfig.Cors.Options.AllowSites.Contains<string>("*"))
            {
                StringValues origins;
                if (context.Request.Headers.TryGetValue("Origin", out origins))
                {
                    foreach (string item in origins)
                    {
                        if (mConfig.Cors.Options.AllowSites.Contains<string>(item,
                            StringComparer.OrdinalIgnoreCase))
                        {
                            context.Response.Headers.Add("Access-Control-Allow-Origin", item);
                            break;
                        }
                    }
                }
            }
            else
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            }

            string allowMethods = "*";
            if (mConfig.Cors.Options.AllowMethods != null)
            {
                allowMethods = string.Join(",", mConfig.Cors.Options.AllowMethods).ToUpper();
            }
            if (allowMethods.Contains("*"))
            {
                allowMethods = "GET,POST,HEAD,PATCH,PUT,DELETE,OPTIONS,TRACE,CONNECT";
            }
            context.Response.Headers.Add("Access-Control-Allow-Methods", allowMethods);

            context.Response.Headers.Add("Access-Control-Allow-Headers", 
                context.Request.Headers["Access-Control-Request-Headers"]);

            context.Response.Headers.Add("Access-Control-Allow-Credentials", 
                mConfig.Cors.Options.SupportsCredentials ? "true" : "false");

            if ("OPTIONS".Equals(context.Request.Method.ToUpper()))
            {
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
