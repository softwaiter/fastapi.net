# 定时任务

虽然我们通过框架开发的 HTTP Server 是请求响应模型的，但是仍然还会有许多场景需要执行一些定时任务，例如：

1. 定时上报应用状态。
2. 定时从远程接口更新本地缓存。
3. 定时进行文件切割、临时文件删除。

框架提供了一套机制来让定时任务的编写和维护更加优雅。



## 编写定时任务

所有的定时任务都统一存放在 `Schedules` 目录下，每一个文件都是一个独立的定时任务，可以配置定时任务的属性和要执行的方法。 

定时任务的实现基于开源的Quartz作业调度框架，所有的定时任务实现类都需要实现IJob接口。

一个简单的例子，我们定义一个定时打印时间的任务，就可以在Schedules目录下创建一个DemoJob.cs类文件

```c#
public class DemoJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine("Hello，现在时间是 " + DateTime.Now);
        return Task.CompletedTask;
    }
}
```

上面的代码实现了定时打印时间的任务，但此时任务还不会运行，必须在schedule.xml中进行任务配置才能正常启动定时任务。



## 配置定时任务

 定时任务的配置全部在schedule.xml文件中进行定义。
 定时任务可以指定 interval 或者 cron 两种不同的定时方式。
 #### interval

```xml
<schedules>
    <job id="demo" interval="10s" repeat="100" class="CodeM.FastApi.Schedules.DemoJob" />
</schedules>
```

id：定时任务唯一编码。

interval：定时任务的间隔时间，支持的单位：ms（毫秒）、s（秒）、m（分钟）、h（小时）、d（天）。

repeat：定时任务执行的次数，默认为0（无限次）。

class：定时任务的实现类全名称。



 #### cron

 通过cron参数来配置定时任务的执行时机，定时任务将会按照 cron 表达式在特定的时间点执行。 

```xml
<schedules>
    <job id="demo" cron="0 0 */3 * * *" class="CodeM.FastApi.Schedules.DemoJob" />
</schedules>
```

id：定时任务唯一编码。

cron：cron表达式，具体cron表达式格式可百度搜索学习。

class：定时任务的实现类全名称。



#### 其他参数

disable： 配置该参数为 true 时，定时任务不会被启动；后续可通过代码进行手动启动。

env： 仅在指定的环境下才启动该定时任务，多个环境逗号分隔。



## 手动执行定时任务
多有的定时任务在启动后都可以通过代码的方式进行控制；需要注意的是，env运行环境不匹配的定时任务，无法启动和控制。

系统提供了Run、Shutdown、ResumeAll、PauseAll、StartJob、StopJob等方法进行定时任务的启动、停止等控制。

###### bool Run()

描述：启动加载后的所有disable为false并且匹配当前env运行环境的定时任务。

参数：无。

返回：成功返回true；否则，返回false。



###### bool Shutdown()

描述：终止运行所有的定时任务，并清理释放相关资源；终止后无法重新启动启动。

参数：无。

返回：成功返回true；否则，返回false。



###### bool ResumeAll()

描述：恢复所有暂定运行的定时任务。

参数：无。

返回：成功返回true；否则，返回false。



###### bool PauseAll()

描述：暂停所有运行中的定时任务；暂停后，可通过ResumeAll恢复运行。

参数：无。

返回：成功返回true；否则，返回false。



###### bool StartJob(string jobId)

描述：启动运行指定Id的定时任务。

参数：

​		jobId：要启动的定时任务的Id。

返回：成功返回true；否则，返回false。



###### bool StopJob(string jobId)

描述：停止运行指定Id的定时任务。

参数：

​		jobId：要启动的定时任务的Id。

返回：成功返回true；否则，返回false。



实际开发中，可以通过App全局对象获取系统的定时任务管理实例。
```c#
//启动Id为demo的定时任务
Application.Instance().Schedule().StartJob("demo");

//停止Id为demo的定时任务
Application.Instance().Schedule().StopJob("demo");
```

