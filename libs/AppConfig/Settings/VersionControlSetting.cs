namespace CodeM.FastApi.Config.Settings
{
    public class VersionControlSetting
    {
        //是否启用版本控制，默认false
        public bool Enable
        {
            get;
            set;
        } = false;

        public string Default
        {
            get;
            set;
        } = "v1";

        public string[] AllowedVersions
        {
            get;
            set;
        }

        public string Param
        {
            get;
            set;
        } = "version";
    }
}
