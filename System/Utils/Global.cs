using CodeM.FastApi.Config;

namespace CodeM.FastApi.System.Utils
{
    public class Global
    {
        private static ApplicationConfig sAppConfig = null;

        public static void Init(ApplicationConfig cfg)
        {
            sAppConfig = cfg;
        }

        public static ApplicationConfig AppConfig
        {
            get
            {
                return sAppConfig;
            }
        }
    }
}
