using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Logger;
using CodeM.FastApi.Middlewares;
using CodeM.FastApi.Router;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CodeM.FastApi
{
    public class Startup
    {

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            try
            {
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

                if (appConfig.Cors.Enable)
                {
                    app.UseMiddleware<CorsMiddleware>(appConfig);
                }

                string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
                RouterManager.Current.Init(appConfig, routerFile);
                RouterManager.Current.MountRouters(app);
            }
            catch (Exception exp)
            {
                LogUtils.Fatal(exp);
                Thread.Sleep(1000);

                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
