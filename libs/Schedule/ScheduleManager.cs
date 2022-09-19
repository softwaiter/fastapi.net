using CodeM.Common.Ioc;
using CodeM.Common.Tools;
using CodeM.FastApi.Common;
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

        public ScheduleManager(string file)
        {
            Load(file);
            BuildScheduler();
        }

        private void Load(string file)
        {
            List < ScheduleSetting > settings = ScheduleParser.Parse(file);
            sSettings = settings;
        }

        private void BuildScheduler()
        {
            //创建计划任务抽象工厂
            ISchedulerFactory factory = new StdSchedulerFactory();
            // 创建计划任务
            sScheduler = factory.GetScheduler().GetAwaiter().GetResult();

            AttachJobs();
        }

        private void AttachJobs()
        {
            sSettings.ForEach(setting =>
            {
                AttachJob(setting);
            });
        }

        private bool IsMatchEnvironment(string env)
        {
            if (!string.IsNullOrWhiteSpace(env))
            {
                string sCurEnv = FastApiUtils.GetEnvironmentName();
                return env.ToLower().Contains(sCurEnv.ToLower());
            }
            return true;
        }

        private bool AttachJob(ScheduleSetting setting, bool ignoreDisable = false)
        {
            if (IsMatchEnvironment(setting.Environment))
            {
                object jobInst = Wukong.GetSingleObject(setting.Class);
                Type _typ = jobInst.GetType();

                IJobDetail job = JobBuilder.Create(_typ)
                    .WithIdentity(JobKey.Create(setting.Id))
                    .Build();

                ITrigger trigger;
                if (!string.IsNullOrWhiteSpace(setting.Interval))
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(new TriggerKey(setting.Id))
                        .WithDailyTimeIntervalSchedule(builder =>
                        {
                            TimeSpan ts = Xmtool.DateTime().GetTimeSpanFromString(setting.Interval).Value;
                            builder = builder.WithIntervalInSeconds((int)ts.TotalSeconds);
                            if (setting.Repeat > 0)
                            {
                                builder = builder.WithRepeatCount(setting.Repeat);
                            }
                        })
                        .Build();
                }
                else
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(new TriggerKey(setting.Id))
                        .WithCronSchedule(setting.Cron)
                        .Build();
                }

                if (!setting.Disable || ignoreDisable)
                {
                    sScheduler.ScheduleJob(job, trigger);
                    return true;
                }
            }
            return false;
        }

        private ScheduleSetting GetScheduleSetting(string jobId)
        {
            return sSettings.Find(setting =>
            {
                return string.Compare(setting.Id, jobId, true) == 0;
            });
        }

        public bool Run()
        {
            if (!IsShutdown() && !IsRunning())
            {
                sScheduler.Start();
                return true;
            }
            return false;
        }

        public bool Shutdown()
        {
            if (!IsShutdown())
            {
                sScheduler.Shutdown();
                return true;
            }
            return false;
        }

        public bool IsRunning()
        {
            return sScheduler.IsStarted &&
                !sScheduler.InStandbyMode &&
                !sScheduler.IsShutdown;
        }

        public bool IsShutdown()
        {
            return sScheduler.IsShutdown;
        }

        public bool ResumeAll()
        {
            if (!IsShutdown() && sScheduler.InStandbyMode)
            {
                sScheduler.Start();
                return true;
            }
            return false;
        }

        public bool PauseAll()
        {
            if (!IsShutdown() && !sScheduler.InStandbyMode)
            {
                sScheduler.Standby();
                return true;
            }
            return false;
        }

        public bool StartJob(string jobId)
        {
            ScheduleSetting setting = GetScheduleSetting(jobId);
            if (IsMatchEnvironment(setting.Environment))
            {
                return AttachJob(setting, true);
            }
            return false;
        }

        public bool StopJob(string jobId)
        {
            bool bRet = sScheduler.UnscheduleJob(new TriggerKey(jobId)).GetAwaiter().GetResult();
            return bRet;
        }
    }
}
