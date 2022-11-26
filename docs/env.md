# 运行环境
一个Web应用在生命周期内至少会经过开发、测试、生产几个不同的环境，比较完善的过程管理甚至还会包含预发布环境、验收环境等。同一系统在不同的运行环境中会自动加载不同的配置，以适应不同运行环境的差异化。



## 指定运行环境

框架常用的指定运行环境的方法有3种：

1. 在开发调试过程中，可以在`launchSettings.json`中进行如下配置：

   ```json
   {
       "iisSettings": {
           "windowsAuthentication": false,
           "anonymousAuthentication": true,
           "iisExpress": {
               "applicationUrl": "http://localhost:5000",
               "sslPort": 0
           }
       },
       "profiles": {
           "IIS Express": {
               "commandName": "IISExpress",
               "launchBrowser": true,
               "environmentVariables": {
                   "ASPNETCORE_ENVIRONMENT": "Development"	// 指定使用开发环境
               }
           },
           "fastapi": {
               "commandName": "Project",
               "launchBrowser": true,
               "applicationUrl": "http://localhost:5000",
               "environmentVariables": {
                   "ASPNETCORE_ENVIRONMENT": "Development"	// 指定使用开发环境配置
               }
           }
       }
   }
   ```

2. 通过`ASPNETCORE_ENVIRONMENT`环境变量指定运行环境更加方便，比如在生产环境所在的服务器中可以执行如下命令：

   ```shell
   //Windows服务器，指定使用开发环境配置
   setx ASPNETCORE_ENVIRONMENT "Development"
   ```
   ```shell
//MacOS/Linux服务器，指定使用开发环境配置
export ASPNETCORE_ENVIRONMENT=development
   ```
3. 通过命令行运行时指定`env`参数：
   ```shell
//指定使用开发环境配置
   dotnet fastapi.dll env=Development
   ```


## 应用内获取运行环境

1. 获取当前运行环境的名称，可以使用系统提供的获取环境变量的方法：

   ```c#
   string envName = FastApiUtils.GetEnvironmentName();
   ```

2. 判断当前是否某个特定的运行环境，可以使用公共库提供的方法：

   ```c#
   if (FastApiUtils.IsDev())	//是否开发调试环境
   {
       //TODO
   }
   
   if (FastApiUtils.IsProd())	//是否生产环境
   {
       //TODO
   }
   
   if (FastApiUtils.IsEnv("Test"))	//是否测试环境，可判断任何自定义环境名称
   {
       //TODO
   }
   ```



## 运行环境相关配置

不同的运行环境会对应不同的配置，具体请阅读 [配置(Config)](config.md)。 



## 自定义环境

常规开发流程可能不仅仅只有以上几种运行环境，fastapi.net 支持自定义环境来适应自己的开发流程。 

比如，要为开发流程增加集成测试环境 TEST。将 `ASPNETCORE_ENVIRONMENT` 设置成 `test`，启动时会加载 `appsettings.test.json`，此时，使用环境判断方法`FastApiUtils.IsEnv("test")`将返回`true`。