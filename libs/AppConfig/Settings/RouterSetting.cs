namespace CodeM.FastApi.Config.Settings
{
    public class RouterSetting
    {

        //全部路由最大并发处理器数量
        public int MaxConcurrentTotal
        {
            get;
            set;
        } = 200;

        //每个路由最大空闲处理器数量
        public int MaxIdlePerRouter
        {
            get;
            set;
        } = 5;

        //每个路由最大并发处理器数量
        public int MaxConcurrentPerRouter
        {
            get;
            set;
        } = 30;

        //每个路由处理器实例最大使用次数
        public int MaxInvokePerInstance
        {
            get;
            set;
        } = 5000;

    }
}
