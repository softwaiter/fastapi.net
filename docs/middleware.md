# 中间件（Middleware）

中间件(Middleware)是一个可以处理 HTTP 请求或响应的软件管道。中间件可以在HTTP请求进入真正的业务逻辑处理之前对HTTP请求进行必要的身份验证、IP黑名单、URL过滤等等；也可以在业务逻辑处理完成之后对返回的结果进行结构格式化、日志记录等操作。

中间件是有顺序的，HTTP请求到来时，会依据中间件的注册顺序依次通过；返回处理结果时，会按照注册的相反顺序依次通过。整个流程如下图所示：

![]( http://face.app100.info/middleware.png )



## 中间件类型

按照作用范围，中间件分为框架中间件和路由中间件两种。

- 框架中间件 - 对所有的进入框架的HTTP请求起作用。
- 路由中间件 - 只对设置了该中间件的路由起作用。



## 编写中间件

编写中间件没有严格的规范，只需要遵守简单的规则；HTPP请求进入时的拦截方法必须命名为Requset，返回时的拦截方法必须命名为Response；方法有一个唯一参数，类型为[ControllerContext]()。

先看一个简单示例：

```c#
using CodeM.FastApi.Context;
using System;

namespace CodeM.FastApi.System.Middlewares
{
    public class DemoMiddleware
    {
        public bool Request(ControllerContext cc)
        {
            Console.WriteLine("请求进来喽！");
            return true;
        }

        public void Response(ControllerContext cc)
        {
            Console.WriteLine("请求返回啦！");
        }
    }
}
```

可以看到，Request是一个返回bool值的方法，当返回true时，HTPP请求会继续向下进入接下来的中间件或业务逻辑处理模块；返回false的话，会直接向上返回，不会经过业务逻辑处理模块。



## 使用中间件

中间件编写完成后，我们还需要手动挂载，支持以下方式：

#### 在框架中使用中间件

框架中间件需要在appsettings.json中进行配置，方式如下：

```json
{
    "AllowedHosts": "*",

    "Compression": {
        "Enable": true
    },

    // 中间件配置使用类全名称，多个之间用逗号分隔
    "Middlewares": [ "CodeM.FastApi.System.Middlewares.DemoMiddleware" ]
}
```



#### 在路由中使用中间件

路由中间件需要在router.xml中进行配置，方式如下：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<routers>
    <!--中间件配置属性middlewares，内容使用类全名称，多个之间用逗号分隔-->
    <router path="/hello" handler="CodeM.FastApi.Controllers.HelloController.Handle" middlewares="CodeM.FastApi.System.Middlewares.DemoMiddleware"></router>
</routers>
```

