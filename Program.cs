using CodeM.FastApi.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace CodeM.FastApi
{
    public class Program
    {
        private static string mEnv = null;
        private static string mPort = null;

        public static void Main(string[] args)
        {
            try
            {
                foreach (string arg in args)
                {
                    string[] paramValues = arg.Split("=");
                    if (paramValues.Length == 2)
                    {
                        if ("env".Equals(paramValues[0].Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            mEnv = paramValues[1].Trim();
                        }
                        else if ("port".Equals(paramValues[0].Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            mPort = paramValues[1].Trim();
                        }
                    }
                }

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
            finally
            {
                Console.ForegroundColor = ConsoleColor.White;
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
                    if (!string.IsNullOrWhiteSpace(mEnv))
                    {
                        webBuilder = webBuilder.UseEnvironment(mEnv);
                    }

                    if (!string.IsNullOrWhiteSpace(mPort))
                    {
                        webBuilder = webBuilder.UseUrls(string.Concat("http://*:", mPort));
                    }

                    webBuilder.UseStartup<Startup>();
                });
    }

}
