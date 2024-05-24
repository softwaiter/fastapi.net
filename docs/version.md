# 版本控制（Version）



## 哪些场景需要版本控制

版本控制功能是一个使用比较低频的功能。一般用户群体较小、生命周期较短的项目型系统不会用到，开发者也不用关心如何实现；只有符合如下特征的系统需要提前考虑版本控制：一是生命周期较长的产品型系统；二是升级之后老版本不会下线，需要和新版本同时运行一段时间的系统；三是系统本身就需要持续维护不同的版本服务不同的用户群。



## 如何配置版本控制

```json
"VersionControl": {
    "Enable": true,			// 是否开启版本控制
    "Default": "v1",		// 默认版本号，默认v1
    "AllowedVersions": [],	// 允许的版本号，默认为空，允许任意版本号
    "Param": "version"		// 版本参数名，请求时通过Header参数或Query参数传递皆可
}
```



## 框架处理逻辑

1. 不传版本号，将使用默认版本号处理。
2. 设置了允许版本号内容，传递的版本号不在允许的版本号中， 将使用默认版本号处理。
3. 未设置允许版本号内容，传递的版本号不在允许的版本号中， 报处理方法未找到异常。



## 不同版本控制器实现方法

在路由定义中，指定的API处理方法将作为默认版本号的处理方法；其他版本的处理方法根据默认版本号的处理方法名加上版本号进行选择。

假设有如下路由定义：

```xml
<router path="/hello" handler="CodeM.FastApi.Controllers.HelloController.Handle"></router>
```

控制器实现如下：

```c#
namespace CodeM.FastApi.Controllers
{
    public class HelloController : BaseController
    {

        public async Task Handle(ControllerContext cc)
        {
            // TODO
            await cc.JsonAsync("这是v1版本的返回结果。");
        }

        /// <summary>
        /// 可通过传入版本号v2参数，测试版本控制功能
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        public async Task Handle_v2(ControllerContext cc)
        {
            // TODO
            await cc.JsonAsync("这是v2版本的返回结果。");
        }
    }
}
```

当用户使用不同的版本参数访问API时，会根据参数变化返回不同结果：

1. 通过`/hello`路由访问，返回`这是v1版本的返回结果。`；
2. 通过`/hello?version=v2`路由访问，返回`这是v2版本的返回结果。`；
3. 通过`/hello?version=v1`路由访问，返回`这是v1版本的返回结果。`；
4. 通过`/hello?version=v3`路由访问，抛出Handle_v3处理方法未找到异常。



## 为什么不使用新路由代替版本控制

使用新路由会给开发者带来如下麻烦：

1. 每个新路由都需要进行路由的定义，而版本控制不会；
2. 前端同一功能因为版本不同需要调用不同的路由，工作量大幅增加；
3. 当多个API出现版本控制需求时，使用新路由很难统一控制；使用版本控制统一控制参数传递很简单。