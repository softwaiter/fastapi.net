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
        /// 单位：秒
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; }
    }
}
