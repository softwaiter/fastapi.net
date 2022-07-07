using CodeM.FastApi.Config.Settings;
using System.Collections.Generic;

namespace CodeM.FastApi.Config
{
    public class ApplicationConfig
    {
        public CompressionSetting Compression { get; set; } = new CompressionSetting();

        public RouterSetting Router { get; set; } = new RouterSetting();

        public List<string> Middlewares { get; set; } = new List<string>();

        public CookieSetting Cookie { get; set; } = new CookieSetting();

        public CorsSetting Cors { get; set; } = new CorsSetting();

        public SessionSetting Session { get; set; } = new SessionSetting();

        public FileUploadSetting FileUpload { get; set; } = new FileUploadSetting();

        public dynamic Settings { get; set; } = null;

    }
}
