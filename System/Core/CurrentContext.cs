using CodeM.FastApi.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeM.FastApi.System.Core
{
    public static class CurrentContext
    {
        private static IHttpContextAccessor _accessor;

        public static ControllerContext Context
        {
            get
            {
                HttpContext context = _accessor.HttpContext;
                return ControllerContext.FromHttpContext(context,
                    Application.Instance().Config());
            }
        }

        internal static void Configure(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }
    }

    public static class CurrentContextExtensions
    {
        public static void AddHttpContextAccessor(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public static IApplicationBuilder UseCurrentContext(this IApplicationBuilder app)
        {
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            CurrentContext.Configure(httpContextAccessor);
            return app;
        }
    }

}
