using CodeM.FastApi.Config;
using CodeM.FastApi.Schedule;

namespace CodeM.FastApi.System.Utils
{
    public class Global
    {
        private static ApplicationConfig sAppConfig = null;
        private static ScheduleManager sScheduleManager = null;

        public static void Init(ApplicationConfig cfg, string scheduleFile)
        {
            sAppConfig = cfg;
            sScheduleManager = new ScheduleManager(scheduleFile);
        }

        public static ApplicationConfig AppConfig
        {
            get
            {
                return sAppConfig;
            }
        }

        public static ScheduleManager Schedule()
        {
            return sScheduleManager;
        }
    }
}
