using CodeM.Common.Ioc;
using CodeM.Common.Orm;
using CodeM.FastApi.Cache;
using CodeM.FastApi.Config;
using CodeM.FastApi.DbUpgrade;
using CodeM.FastApi.Log;
using CodeM.FastApi.Schedule;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.System.Core
{
    public class App
    {
        private static IWebHostEnvironment sEnv = null;
        private static ApplicationConfig sAppConfig = null;
        private static ScheduleManager sScheduleManager = null;
        private static Regex sThirdDot = new Regex("\\.[^\\.]*\\.[^\\.]*\\.[^\\.]*$");

        private static App sSingleInst = new App();

        public static void Init(IWebHostEnvironment env, 
            ApplicationConfig cfg, string scheduleFile)
        {
            sEnv = env;
            InitOrm(env.ContentRootPath);
            CacheLoader.Load(cfg);

            sAppConfig = cfg;

            Console.WriteLine("加载定时任务配置文件......");
            sScheduleManager = new ScheduleManager(scheduleFile);
        }

        private static void InitOrm(string contentRootPath)
        {
            //ORM模型库初始化
            OrmUtils.ModelPath = Path.Combine(contentRootPath, "models");
            Console.WriteLine("加载ORM模型定义文件：" + OrmUtils.ModelPath);
            OrmUtils.Load();
            OrmUtils.TryCreateTables();

            //ORM版本控制
            UpgradeManager.EnableVersionControl();
            UpgradeManager.Load(Path.Combine(contentRootPath, "models", ".upgrade.xml"));
            Console.WriteLine("执行ORM模型升级操作......");
            UpgradeManager.Upgrade();
        }

        private App()
        {
        }

        public static App GetInstance()
        {
            return sSingleInst;
        }

        public string ContentRootPath
        {
            get
            {
                if (sEnv != null)
                {
                    return sEnv.ContentRootPath;
                }
                return string.Empty;
            }
        }

        private string mAddress = string.Empty;
        public string Address
        {
            get
            {
                return mAddress;
            }
            internal set
            {
                mAddress = value;
            }
        }

        public ApplicationConfig Config()
        {
            return sAppConfig;
        }

        public ScheduleManager Schedule()
        {
            return sScheduleManager;
        }

        public ICache Cache(string cacheName = null)
        {
            return CacheManager.Cache(cacheName);
        }

        public Logger Log()
        {
            return Logger.GetInstance();
        }

        public dynamic Service(string serviceName, bool singleton = true)
        {
            string appFullName = GetType().FullName;
            string serviceFullName = sThirdDot.Replace(appFullName, string.Concat(".Services.", serviceName, "Service"));
            if (singleton)
            {
                return IocUtils.GetSingleObject(serviceFullName);
            }
            else
            {
                return IocUtils.GetObject(serviceFullName);
            }
        }
    }
}
