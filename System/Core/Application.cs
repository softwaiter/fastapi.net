using CodeM.Common.Ioc;
using CodeM.Common.Orm;
using CodeM.FastApi.Cache;
using CodeM.FastApi.Config;
using CodeM.FastApi.DbUpgrade;
using CodeM.FastApi.Log;
using CodeM.FastApi.Schedule;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.System.Core
{
    public class Application
    {
        private static ApplicationConfig sAppConfig = null;
        private static ScheduleManager sScheduleManager = null;
        private static Regex sThirdDot = new Regex("\\.[^\\.]*\\.[^\\.]*\\.[^\\.]*$");

        private static Application sSingleInst = new Application();

        public static void Init(ApplicationConfig cfg, string scheduleFile)
        {
            InitOrm(AppDomain.CurrentDomain.BaseDirectory);
            CacheLoader.Load(cfg);

            sAppConfig = cfg;

            Console.WriteLine("加载定时任务配置文件......");
            sScheduleManager = new ScheduleManager(scheduleFile);
        }

        private static void InitOrm(string contentRootPath)
        {
            //ORM模型库初始化
            Derd.ModelPath = Path.Combine(contentRootPath, "models");
            Console.WriteLine("加载ORM模型定义文件：" + Derd.ModelPath);
            Derd.Load();

            Exception exp;
            if (!Derd.TryCreateTables(false, true, out exp))
            {
                throw exp;
            }

            //ORM版本控制
            UpgradeManager.EnableVersionControl();
            Console.WriteLine("执行ORM模型升级操作......");
            UpgradeManager.Upgrade(Path.Combine(contentRootPath, "models"));
        }

        private Application()
        {
        }

        public static Application Instance()
        {
            return sSingleInst;
        }

        public string ContentRootPath
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
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
            return Logger.Instance();
        }

        public dynamic Service(string serviceName, bool singleton = true)
        {
            string appFullName = GetType().FullName;
            string serviceFullName = sThirdDot.Replace(appFullName, string.Concat(".Services.", serviceName, "Service"));
            if (singleton)
            {
                return Wukong.GetSingleObject(serviceFullName);
            }
            else
            {
                return Wukong.GetObject(serviceFullName);
            }
        }
    }
}
