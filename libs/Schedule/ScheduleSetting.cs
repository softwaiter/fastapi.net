namespace CodeM.FastApi.Schedule
{
    public class ScheduleSetting
    {
        public string Id { get; set; }

        /// <summary>
        /// 重复次数，默认0，不限制
        /// </summary>
        public int Repeat { get; set; } = 0;

        /// <summary>
        /// 轮询周期，默认单位秒，支持ms、s、m、h、d
        /// </summary>
        public string Interval { get; set; }

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; }

        /// <summary>
        /// 定时任务执行器
        /// </summary>
        public string Handler { get; set; }
    }
}
