using CodeM.Common.Orm;
using CodeM.FastApi.DbUpgrade;
using CodeM.FastApi.Log;
using CodeM.FastApi.Log.File;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace CodeM.FastApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception exp)
            {
                if (Logger.Inited)
                {
                    Logger.Create().Fatal(exp);
                    Thread.Sleep(1000);
                }
                
                Process.GetCurrentProcess().Kill();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        logging.AddDebug();
                        logging.AddConsole();
                    }
                    logging.AddFile();

                    ILoggerFactory factory = logging.Services.BuildServiceProvider().GetService<ILoggerFactory>();
                    Logger.Init(factory);

                    InitApp(hostingContext);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void InitApp(HostBuilderContext hostingContext)
        {
            //日志文件写入器初始化
            FileWriter.InitConfig(hostingContext.Configuration);

            //ORM模型库初始化
            OrmUtils.ModelPath = Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "models");
            OrmUtils.Load();
            OrmUtils.TryCreateTables();

            //ORM版本控制
            UpgradeManager.EnableVersionControl();
            UpgradeManager.Load(Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "models", ".upgrade.xml"));
            UpgradeManager.Upgrade();
        }

    }

}
