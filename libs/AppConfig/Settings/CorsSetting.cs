namespace CodeM.FastApi.Config.Settings
{
    public class CorsSetting
    {
        public class CorsSettingOptions
        {
            public string[] AllowSites
            {
                get;
                set;
            }

            public string[] AllowMethods
            {
                get;
                set;
            }

            public bool SupportsCredentials
            {
                get;
                set;
            } = false;
        }

        //是否启用跨域设置，默认false
        public bool Enable
        {
            get;
            set;
        } = false;

        public CorsSettingOptions Options
        {
            get;
            set;
        } = new CorsSettingOptions();
    }
}
