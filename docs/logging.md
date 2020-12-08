# 日志

日志对于 Web 开发的重要性毋庸置疑，它对于监控应用的运行状态、问题排查等都有非常重要的意义。

框架基于微软日志工厂进行封装，同时增加日志写入文件能力。

主要特性：

- 开发和生产智能区分
- 日志分级
- 自动分割日志
- 高性能



## 开发和生产智能区分

基于开发环境和生产环境的不同需要，框架直接硬编码对两种环境进行了区分。

1. 开发环境默认仅支持控制台、终端 2种日志同时输出。

2. 开发环境如需输出到日志文件，可通过在appsettings.json中配置：

   ```json
   "Logging": {
       "File": {
           "LogLevel": {
               "Default": "Debug"
           }
       }
   }
   ```

3. 生产环境仅支持文件日志的输出，且输出日志级别为Warning，如需在生产环境打印跟踪调试信息，可在appsettings.json中调整日志文件的输出级别：

   ```json
   "Logging": {
       "File": {
           "LogLevel": {
               "Default": "Trace"
           }
       }
   }
   ```

   

## 日志分级

框架使用微软日志工厂基本能力封装而成，沿用了日志工厂的分级方式，分为None、Trace、Debug、Information、Warning、Error、Fatal 7个级别。

- None - 不打印
- Trace - 跟踪日志，级别最低，一般不会使用。
- Debug - 开发调试日志，通常用于开发过程打印运行信息。
- Information - 系统运行情况日志，粒度相较于Debug更粗，记录系统重要信息。
- Warning - 警告信息，指示有潜在错误或可能有风险的信息。
- Error - 错误信息，但不影响系统继续运行。
- Fatal - 致命错误信息，会影响系统运行，甚至终止运行。



## 日志文件路径

框架默认的日志文件保存路径为当前工作目录的logs子目录下。

日志文件默认命名为fastapi.log，实际写入名称会根据日志文件分割方式增加不同的后缀。如按天分割：fastapi_yyyy-MM-dd.log。

如果想自定义日志路径，在appsettings.json中进行如下配置：

```json
"Logging": {
    "File": {
        "Options": {
            "FileName": "D:\\fastapi.log"
        }
    }
}
```



## 日志文件格式

日志文件采用文本文件格式保存，具体格式如下：

级别: 日志写入位置 - 记录时间

​		 日志内容

如：

```
info: CodeM.FastApi.Program.InitApp[0] - 2020/9/21 15:01:43
     Hello World.
```



## 日志文件分割

为避免日志文件一直写入，框架支持按照多种方式对日志文件进行自动分割。

- Date - 按日期进行分割，一天的日志写入一个文件。
- Hour - 按小时进行分割，一小时的日志写入一个文件。
- Size - 按文件大小进行分割，日志内容每达到一定大小就写入一个文件。
- None - 不分割。



框架默认分割设置为None，不进行分割，用户可在appsettings.json中进行自定义：

```json
"Logging": {
    "File": {
        "Options": {
            "SplitType": "Size",
            "MaxFileSize": 1048576	//单位为字节；如果不设置，默认为2097152，即2M；其他分割类型无需设置该配置项。
        }
    }
}
```



## 日志文件滚动个数

使用了日志分割配置后，框架会根据分割规则将日志分割为若干个小文件，随着日志的增加，小文件会越来越多，为了避免小文件太多占用过多的硬盘空间，可以通过配置日志文件滚动个数，只保存最新的几个日志文件，框架默认为10，即保存离当前时间点最近的10个日志文件。

如需更改，可在appsettings.json中配置：

```json
"Logging": {
    "File": {
        "Options": {
            "MaxFileBackups": 20
        }
    }
}
```



## 日志文件编码

日志文件默认采用UTF8编码，如果需要其他格式的编码，可以在appsettings.json中修改：

```json
"Logging": {
    "File": {
        "Options": {
            "Encoding": "gb2312"
        }
    }
}
```



## 如何打印日志

在框架层面，用户可以全程获得ControllerContext对象，该对象提供方法可以方便的打印日志：

```c#
public async Task Handle(ControllerContext cc)
{
    cc.Debug("Hello World.");  //打印一条调试日志
    await cc.JsonAsync("Hello World.");
}
```

- ControllerContext.Trace - 打印跟踪日志
- ControllerContext.Debug - 打印调试日志
- ControllerContext.Info - 打印运行信息
- ControllerContext.Warn - 打印警告日志
- ControllerContext.Error - 打印错误日志
- ControllerContext.Fatal - 打印致命错误日志



## 高性能

通常 Web 访问是高频访问，每次打印日志都写磁盘会造成频繁磁盘 IO，为了提高性能，我们采用的文件日志写入策略是：

> 日志同步写入内存，异步每隔一段时间(默认 1 秒)刷盘