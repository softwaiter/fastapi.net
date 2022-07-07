namespace CodeM.FastApi.Config.Settings
{
    public class FileUploadSetting
    {
        public long MaxBodySize { get; set; } = 1024 * 1024 * 5;    // 默认5M
    }
}
