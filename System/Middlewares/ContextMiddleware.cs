using CodeM.FastApi.Config;
using CodeM.FastApi.System.Core;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CodeM.FastApi.System.Middlewares
{
    public class ContextMiddleware
    {
        private readonly RequestDelegate _next;
        private ApplicationConfig mConfig;

        public ContextMiddleware(RequestDelegate next, ApplicationConfig config)
        {
            _next = next;
            mConfig = config;
        }

        public async Task Invoke(HttpContext context)
        {
            CurrentContext.Add(context);

            await _next(context);

            CurrentContext.Remove();
        }
    }
}