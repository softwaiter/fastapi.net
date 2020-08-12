using CodeM.FastApi.Config;
using CodeM.FastApi.Router;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace fastapi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            AppConfig appConfig = new AppConfig();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(appConfig);

            string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
            RouterManager.Current.Init(appConfig, routerFile);
            RouterManager.Current.MountRouters(app);
        }
    }
}
