using CodeM.Common.Tools;
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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
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
            Init(env);
        }

        private void Init(IWebHostEnvironment env)
        {
            Console.WriteLine("解析框架全局配置文件......");
            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile(string.Concat("appsettings.", env.EnvironmentName, ".json"), true, true);
            builder.Build().Bind(AppConfig);

            string settingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string envSettingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Concat("appsettings.", env.EnvironmentName, ".json"));
            AppConfig.Settings = Xmtool.Json.ConfigParser()
                .AddJsonFile(settingFile)
                .AddJsonFile(envSettingFile)
                .Parse();
        }

        internal ApplicationConfig AppConfig { get; set; } = new ApplicationConfig();

        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("初始化框架配置信息......");

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            if (AppConfig.Compression.Enable)
            {
                services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.Providers.Add<GzipCompressionProvider>();
                });
            }

            if (AppConfig.Session.Enable)
            {
                if (AppConfig.Session.Redis.Enable)
                {
                    services.AddStackExchangeRedisCache(options =>
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

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = AppConfig.FileUpload.MaxBodySize;
            });

            string scheduleFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schedule.xml");
            Application.Init(AppConfig, scheduleFile);
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

                app.Use(next => context =>
                {
                    context.Request.EnableBuffering();
                    return next(context);
                });

                app.UseCurrentContext();

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
                string routerFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "router.xml");
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
                        Application.Instance().Address = listenAddress;
                    }

                    Console.WriteLine(string.Format("启动成功，监听地址：[{0}]，环境变量：[{1}]",
                        listenAddress, env.EnvironmentName));
                    Console.WriteLine("===================================================================================");
                    Console.ForegroundColor = ConsoleColor.White;
                });
                lifetime.ApplicationStopping.Register(() =>
                {
                    Application.Instance().Schedule().Shutdown();
                });
                Application.Instance().Schedule().Run();
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.White;

                Logger.Instance().Fatal(exp);
                Thread.Sleep(1000);

                Process.GetCurrentProcess().Kill();
            }
        }
    }
}
