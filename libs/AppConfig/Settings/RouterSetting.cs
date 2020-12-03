using System;

namespace CodeM.FastApi.Config.Settings
{
    public class RouterSetting
    {

        private int mMaxConcurrentTotal = 65535;
        //全部路由最大并发处理器数量
        public int MaxConcurrentTotal
        {
            get
            {
                return mMaxConcurrentTotal;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception(string.Concat("Router配置项MaxConcurrentTotal必须大于等于0：", value));
                }
                mMaxConcurrentTotal = value;
            }
        }

        private int mMaxIdlePerRouter = 10;
        //每个路由最大空闲处理器数量
        public int MaxIdlePerRouter
        {
            get
            {
                return mMaxIdlePerRouter;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception(string.Concat("Router配置项MaxIdlePerRouter必须大于等于0：", value));
                }
                mMaxIdlePerRouter = value;
            }
        }

        private int mMaxConcurrentPerRouter = 100;
        //每个路由最大并发处理器数量
        public int MaxConcurrentPerRouter
        {
            get
            {
                return mMaxConcurrentPerRouter;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception(string.Concat("Router配置项MaxConcurrentPerRouter必须大于等于0：", value));
                }
                mMaxConcurrentPerRouter = value;
            }
        }

        private int mMaxInvokePerInstance = 10000;
        //每个路由处理器实例最大使用次数
        public int MaxInvokePerInstance
        {
            get
            {
                return mMaxInvokePerInstance;
            }
            set
            {
                if (value < 0)
                {
                    throw new Exception(string.Concat("Router配置项MaxInvokePerInstance必须大于等于0：", value));
                }
                mMaxInvokePerInstance = value;
            }
        }

    }
}
