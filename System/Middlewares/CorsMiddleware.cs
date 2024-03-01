using CodeM.FastApi.Config;
using CodeM.FastApi.System.Utils;
using Microsoft.AspNetCore.Http;
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
            CorsUtils.SetCorsHeaders(context);

            if ("OPTIONS".Equals(context.Request.Method.ToUpper()))
            {
                return Task.CompletedTask;
            }

            return _next(context);
        }
    }
}
