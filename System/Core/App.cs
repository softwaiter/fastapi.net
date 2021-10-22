using CodeM.FastApi.Config;
using CodeM.FastApi.Log;
using CodeM.FastApi.Schedule;

namespace CodeM.FastApi.System.Core
{
    public class App
    {
        private static ApplicationConfig sAppConfig = null;
        private static ScheduleManager sScheduleManager = null;

        private static App sSingleInst = new App();

        public static void Init(ApplicationConfig cfg, string scheduleFile)
        {
            sAppConfig = cfg;
            sScheduleManager = new ScheduleManager(scheduleFile);
        }

        private App()
        {
        }

        public static App Create()
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
            return Logger.Create();
        }
    }
}
