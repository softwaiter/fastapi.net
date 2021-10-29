using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Log;
using CodeM.FastApi.Router;
using CodeM.FastApi.System.Core;
using CodeM.FastApi.System.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CodeM.FastApi
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            this.Environment = env;
            Init(env);
        }

        private IWebHostEnvironment Environment { get; set; }

        private void Init(IWebHostEnvironment env)
        {
            Console.WriteLine("解析框架全局配置文件......");
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
            Console.WriteLine("初始化框架配置信息......");

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

            string scheduleFile = Path.Combine(this.Environment.ContentRootPath, "schedule.xml");
            App.Init(this.Environment, AppConfig, scheduleFile);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IServer server, IHostApplicationLifetime lifetime)
        {
            try
            {
                if (env.IsDevelopment())
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

                Console.WriteLine("挂载API路由接口......");
                string routerFile = Path.Combine(env.ContentRootPath, "router.xml");
                RouterManager.Current.Init(AppConfig, routerFile);
                RouterManager.Current.MountRouters(app);

                Console.WriteLine("启动定时任务调度程序......");
                lifetime.ApplicationStarted.Register(() =>
                {
                    string listenAddress = string.Empty;
                    IServerAddressesFeature saf = server.Features.Get<IServerAddressesFeature>();
                    if (saf.Addresses.Count > 0)
                    {
                        IEnumerator<string> e = saf.Addresses.GetEnumerator();
                        if (e.MoveNext())
                        {
                            listenAddress = e.Current.Trim();
                        }
                        App.GetInstance().Address = listenAddress;
                    }

                    Console.WriteLine(string.Format("启动成功，监听地址：[{0}]，环境变量：[{1}]",
                        listenAddress, env.EnvironmentName));
                    Console.WriteLine("===================================================================================");
                    Console.ForegroundColor = ConsoleColor.White;
                });
                lifetime.ApplicationStopping.Register(() =>
                {
                    App.GetInstance().Schedule().Shutdown();
                });
                App.GetInstance().Schedule().Run();
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.White;

                Logger.GetInstance().Fatal(exp);
                Thread.Sleep(1000);

                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
