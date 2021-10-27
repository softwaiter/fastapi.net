using CodeM.Common.Orm;
using CodeM.FastApi.DbUpgrade;
using CodeM.FastApi.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                string frameworkName = " _____      ___   _____   _____       ___   _____   _       __   _   _____   _____  \n" +
                                       "|  ___|    /   | /  ___/ |_   _|     /   | |  _  \\ | |     |  \\ | | | ____| |_   _| \n" +
                                       "| |__     / /| | | |___    | |      / /| | | |_| | | |     |   \\| | | |__     | |   \n" +
                                       "|  __|   / /_| | \\___  \\   | |     / /_| | |  ___/ | |     | |\\   | |  __|    | |   \n" +
                                       "| |     / /  | |  ___| |   | |    / /  | | | |     | |  _  | | \\  | | |___    | |   \n" +
                                       "|_|    /_/   |_| /_____/   |_|   /_/   |_| |_|     |_| |_| |_|  \\_| |_____|   |_|   \n" +
                                       "                                                                                    \n" +
                                       "===================================================================================\n";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(frameworkName);
                Console.ForegroundColor = ConsoleColor.Black;

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception exp)
            {
                if (Logger.Inited)
                {
                    Logger.GetInstance().Fatal(exp);
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

                    IConfigurationSection options = hostingContext.Configuration
                        .GetSection("Logging").GetSection("File").GetSection("Options");
                    logging.AddFile(options);

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
            //ORM模型库初始化
            OrmUtils.ModelPath = Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "models");
            Logger.GetInstance().Info("加载ORM模型定义文件：" + OrmUtils.ModelPath);
            OrmUtils.Load();
            OrmUtils.TryCreateTables();

            //ORM版本控制
            UpgradeManager.EnableVersionControl();
            UpgradeManager.Load(Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "models", ".upgrade.xml"));
            Logger.GetInstance().Info("执行ORM模型的升级逻辑......");
            UpgradeManager.Upgrade();
        }

    }

}
