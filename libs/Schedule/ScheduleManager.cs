using System;
using System.Collections.Generic;

namespace CodeM.FastApi.Schedule
{
    public class ScheduleManager
    {
        private static List<ScheduleSetting> sSettings;

        public static void Load(string file)
        {
            List < ScheduleSetting > settings = ScheduleParser.Parse(file);
            sSettings = settings;
        }

        private static void CheckSettingData()
        {
            if (sSettings == null)
            {
                throw new Exception("请先加载任务配置数据。");
            }
        }

        public static bool Start()
        {
            CheckSettingData();
            return true;
        }

        public static bool Pause()
        {
            CheckSettingData();
            return true;
        }

        public static bool Resume()
        {
            CheckSettingData();
            return true;
        }

        public static bool Stop()
        {
            CheckSettingData();
            return true;
        }
    }
}
