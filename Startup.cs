using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Log;
using CodeM.FastApi.Router;
using CodeM.FastApi.System.Core;
using CodeM.FastApi.System.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            Logger.GetInstance().Info("解析框架配置文件......");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(AppConfig);

            string settingFile = Path.Combine(env.ContentRootPath, "appsettings.json");
            string envSettingFile = Path.Combine(env.ContentRootPath, string.Concat("appsettings.", env.EnvironmentName, ".json"));
            Json2Dynamic j2d = new Json2Dynamic().AddJsonFile(settingFile).AddJsonFile(envSettingFile);
            AppConfig.Settings = j2d.Parse();

            string scheduleFile = Path.Combine(env.ContentRootPath, "schedule.xml");

            Logger.GetInstance().Info("加载定时任务配置文件......");
            App.Init(AppConfig, scheduleFile);
        }

        internal ApplicationConfig AppConfig { get; set; } = new ApplicationConfig();

        public void ConfigureServices(IServiceCollection services)
        {
            Logger.GetInstance().Info("初始化框架配置信息......");

            if (AppConfig.Compression.Enable)
            {
                services.AddResponseCompression();
            }

            if (AppConfig.Session.Enable)
            {
                if (AppConfig.Session.Redis.Enable)
                {
                    services.AddDistributedRedisCache(options =>
                    {
                        options.Configuration = AppConfig.Session.Redis.ToString();
                        options.InstanceName = AppConfig.Session.Redis.InstanceName;
                    });
                }
                else
                {
                    services.AddDistributedMemoryCache();
                }

                services.AddSession(options =>
                {
                    options.IdleTimeout = AppConfig.Session.TimeoutTimeSpan;

                    options.Cookie.Name = AppConfig.Session.Cookie.Name;
                    options.Cookie.HttpOnly = AppConfig.Session.Cookie.HttpOnly;
                    options.Cookie.SameSite = AppConfig.Session.Cookie.SameSite;
                    options.Cookie.SecurePolicy = AppConfig.Session.Cookie.Secure;
                    options.Cookie.MaxAge = AppConfig.Session.Cookie.MaxAgeTimeSpan;
                    options.Cookie.IsEssential = true;
                });

                services.Configure<CookiePolicyOptions>(options =>
                {
                    options.CheckConsentNeeded = context => false;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            try
            {
                if ("Development".Equals(env.EnvironmentName, StringComparison.OrdinalIgnoreCase))
                {
                    app.UseDeveloperExceptionPage();
                }

                if (AppConfig.Compression.Enable)
                {
                    app.UseResponseCompression();
                }

                if (AppConfig.Session.Enable)
                {
                    app.UseSession();
                }

                if (AppConfig.Cors.Enable)
                {
                    app.UseMiddleware<CorsMiddleware>(AppConfig);
                }

                Logger.GetInstance().Info("挂载API路由接口......");
                string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
                RouterManager.Current.Init(AppConfig, routerFile);
                RouterManager.Current.MountRouters(app);

                Logger.GetInstance().Info("启动定时任务......");
                lifetime.ApplicationStopping.Register(() =>
                {
                    App.GetInstance().Schedule().Shutdown();
                });
                App.GetInstance().Schedule().Run();
            }
            catch (Exception exp)
            {
                Logger.GetInstance().Fatal(exp);
                Thread.Sleep(1000);

                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
