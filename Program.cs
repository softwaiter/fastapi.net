using CodeM.Common.Orm;
using CodeM.FastApi.Logger.File;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CodeM.FastApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    InitApp(hostingContext);

                    logging.ClearProviders();
                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        logging.AddDebug();
                        logging.AddConsole();
                    }
                    logging.AddFile();
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
            OrmUtils.CreateTables();
        }

    }

}
