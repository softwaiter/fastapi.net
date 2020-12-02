using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Logger;
using CodeM.FastApi.Middlewares;
using CodeM.FastApi.Router;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        public Startup(IWebHostEnvironment env)
        {
            Init(env);
        }

        private void Init(IWebHostEnvironment env)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(AppConfig);

            string settingFile = Path.Combine(env.ContentRootPath, "appsettings.json");
            string envSettingFile = Path.Combine(env.ContentRootPath, string.Concat("appsettings.", env.EnvironmentName, ".json"));
            Json2Dynamic j2d = new Json2Dynamic().AddJsonFile(settingFile).AddJsonFile(envSettingFile);
            AppConfig.Settings = j2d.Parse();
        }

        internal ApplicationConfig AppConfig { get; set; } = new ApplicationConfig();

        public void ConfigureServices(IServiceCollection services)
        {
            if (AppConfig.Session.Enable)
            {
                services.AddDistributedMemoryCache();
                services.AddSession(options =>
                {
                    options.IdleTimeout = AppConfig.Session.Options.TimeoutTimeSpan;

                    options.Cookie.Name = AppConfig.Session.Options.Cookie.Name;
                    options.Cookie.HttpOnly = AppConfig.Session.Options.Cookie.HttpOnly;
                    options.Cookie.SameSite = AppConfig.Session.Options.Cookie.SameSite;
                    options.Cookie.MaxAge = AppConfig.Session.Options.Cookie.MaxAgeTimeSpan;
                    options.Cookie.IsEssential = true;
                });

                services.Configure<CookiePolicyOptions>(options =>
                {
                    options.CheckConsentNeeded = context => false;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            try
            {
                if (AppConfig.Session.Enable)
                {
                    app.UseSession();
                }

                if (AppConfig.Cors.Enable)
                {
                    app.UseMiddleware<CorsMiddleware>(AppConfig);
                }

                string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
                RouterManager.Current.Init(AppConfig, routerFile);
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
