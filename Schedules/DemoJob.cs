using Quartz;
using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Schedules
{
    public class DemoJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Hello，现在时间是 " + DateTime.Now);
            return Task.CompletedTask;
        }
    }
}
