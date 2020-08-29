using CodeM.Common.Tools.Config;
using CodeM.FastApi.Config;
using CodeM.FastApi.Router;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CodeM.FastApi
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory factory)
        {
            AppConfig appConfig = new AppConfig();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(appConfig);

            string settingFile = Path.Combine(env.ContentRootPath, "appsettings.json");
            string envSettingFile = Path.Combine(env.ContentRootPath, string.Concat("appsettings.", env.EnvironmentName, ".json"));
            JsonConfigParser configParser = new JsonConfigParser().AddJsonFile(settingFile).AddJsonFile(envSettingFile);
            appConfig.Settings = configParser.Parse();

            string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
            RouterManager.Current.Init(appConfig, routerFile);
            RouterManager.Current.MountRouters(app);
        }
    }
}
