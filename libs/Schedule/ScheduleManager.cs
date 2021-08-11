using CodeM.Common.Ioc;
using CodeM.Common.Tools;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;

namespace CodeM.FastApi.Schedule
{
    public class ScheduleManager
    {
        private static List<ScheduleSetting> sSettings;

        private static IScheduler sScheduler;

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

        private static void RunAllJobs()
        {
            sSettings.ForEach(setting =>
            {
                object jobInst = IocUtils.GetSingleObject(setting.Class);
                Type _typ = jobInst.GetType();

                IJobDetail job = JobBuilder.Create(_typ).WithIdentity(setting.Id).Build();

                ITrigger trigger;
                if (!string.IsNullOrWhiteSpace(setting.Interval))
                {
                    trigger = TriggerBuilder.Create().WithDailyTimeIntervalSchedule(builder =>
                    {

                        TimeSpan ts = DateTimeUtils.GetTimeSpanFromString(setting.Interval).Value;
                        builder = builder.WithIntervalInSeconds((int)ts.TotalSeconds);
                        if (setting.Repeat > 0)
                        {
                            builder = builder.WithRepeatCount(setting.Repeat);
                        }
                    }).Build();
                }
                else
                {
                    trigger = TriggerBuilder.Create().WithCronSchedule(setting.Cron).Build();
                }

                sScheduler.ScheduleJob(job, trigger);
            });
        }

        public static bool StartAll()
        {
            CheckSettingData();

            if (sScheduler == null)
            {
                //创建计划任务抽象工厂
                ISchedulerFactory factory = new StdSchedulerFactory();
                // 创建计划任务
                sScheduler = factory.GetScheduler().GetAwaiter().GetResult();

                RunAllJobs();

                sScheduler.Start();

                return true;
            }

            return false;
        }

        public static bool PauseAll()
        {
            CheckSettingData();

            if (sScheduler != null && sScheduler.IsStarted)
            {
                sScheduler.PauseAll();
                return true;
            }

            return false;
        }

        public static bool ResumeAll()
        {
            CheckSettingData();

            if (sScheduler != null && !sScheduler.IsStarted)
            {
                sScheduler.ResumeAll();
                return true;
            }

            return false;
        }

        public static bool StopAll()
        {
            CheckSettingData();

            if (sScheduler != null && !sScheduler.IsShutdown)
            {
                sScheduler.Shutdown();
                sScheduler.Clear();
                sScheduler = null;

                return true;
            }

            return false;
        }
    }
}
