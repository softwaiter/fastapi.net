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
                                       "===================================================================================";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(frameworkName);

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
                    Console.WriteLine("开始启动......");
                    Console.WriteLine(string.Format("发现内容目录：{0}",
                        hostingContext.HostingEnvironment.ContentRootPath));

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
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

}
