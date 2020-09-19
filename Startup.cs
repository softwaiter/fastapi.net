using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Logger;
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
            LogUtils.Init(factory);

            AppConfig appConfig = new AppConfig();

            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(appConfig);

            string settingFile = Path.Combine(env.ContentRootPath, "appsettings.json");
            string envSettingFile = Path.Combine(env.ContentRootPath, string.Concat("appsettings.", env.EnvironmentName, ".json"));
            Json2Dynamic j2d = new Json2Dynamic().AddJsonFile(settingFile).AddJsonFile(envSettingFile);
            appConfig.Settings = j2d.Parse();

            string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
            RouterManager.Current.Init(appConfig, routerFile);
            RouterManager.Current.MountRouters(app);
        }
    }
}
