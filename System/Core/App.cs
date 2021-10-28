using CodeM.Common.Ioc;
using CodeM.FastApi.Config;
using CodeM.FastApi.Log;
using CodeM.FastApi.Schedule;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.System.Core
{
    public class App
    {
        private static ApplicationConfig sAppConfig = null;
        private static ScheduleManager sScheduleManager = null;
        private static Regex sThirdDot = new Regex("\\.[^\\.]*\\.[^\\.]*\\.[^\\.]*$");

        private static App sSingleInst = new App();

        public static void Init(ApplicationConfig cfg, string scheduleFile)
        {
            sAppConfig = cfg;
            sScheduleManager = new ScheduleManager(scheduleFile);
        }

        private App()
        {
        }

        public static App GetInstance()
        {
            return sSingleInst;
        }

        public ApplicationConfig Config()
        {
            return sAppConfig;
        }

        public ScheduleManager Schedule()
        {
            return sScheduleManager;
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
