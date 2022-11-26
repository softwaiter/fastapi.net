# Config 配置

框架提供了强大且可扩展的配置功能，支持根据运行环境维护不同的配置；运行时，框架会自动合并默认配置和运行环境的配置，合并后的配置可直接从 `Application.Instance().Config()` 获取。 

## 多环境配置

框架支持根据环境来加载配置，用户可以定义多个环境的配置文件，实现一次构建多次部署，具体环境请查看[[运行环境配置]](env.md)

```json
appsettings.json
appsettings.Development.json
appsettings.Production.json
```

`appsettings.json` 为默认的配置文件，所有环境都会加载这个配置文件，一般所有环境都需要且内容相同的配置会放在这个配置文件中。 

当指定env时，框架会首先加载默认配置文件`appsettings.json`，然后加载对应运行环境的配置文件，具体运行环境配置文件中的配置项会覆盖默认配置文件中的同名配置项。如Production环境会加载appsettings.json配置文件和appsettings.Production.json配置文件，appsettings.Production.json会覆盖appsettings.json的同名配置。

## 配置写法

配置文件内容是一个JSON格式的对象，可以覆盖框架的一些配置，应用也可以将自己业务的配置放到这里方便管理。 

```json
// 配置允许访问的Host主机地址
{
    "AllowedHosts": "127.0.0.1"
}
```

## 配置内容

配置内容主要包含框架配置和用户自定义的业务配置，如下是框架自身的主要配置：

```json
{
    "AllowedHosts": "*",	//允许访问的主机地址，默认为*，即无限制。

    "Compression": {
        "Enable": true	//请求返回是否启用压缩。
    },

    "Middlewares": [],	//框架中间件，多个用逗号分隔。

    "Router": {
        "MaxConcurrentTotal": 65535,	//框架的最大并发数。
        "MaxIdlePerRouter": 10,	//每个路由池中保留的最大空闲实例数。
        "MaxConcurrentPerRouter": 100,	//每个路由的最大并发数。
        "MaxInvokePerInstance": 10000	//每个路由实例处理请求的次数上限，超过将销毁实例。
    },

    "Cookie": {
        "Keys": "fastapi"	//读写cookie的密钥，多个用逗号分隔。
        					//加密时使用第一个，解密时按照逗号分隔依次尝试。
    },

    "Session": {
        "Enable": true,	//是否启用session会话机制。
        "Timeout": "20m",	//session超时时间。
        "Cookie": {
            "Name": "fastapi.sid",	//session对应的cookie名称。
            "SameSite": "None",	//session对应cookie的SameSite配置。
            "HttpOnly": true,	//session对应cookie的HttpOnly配置。
            "Secure":  "None"	//session对应cookie的Secure配置。
        },
        "Redis": {
            "Enable": false	//是否使用redis存储session数据。
        }
    },

    "Cors": {
        "Enable": true,	//是否启用跨域安全检查。
        "Options": {
            "AllowMethods": [ "*" ],	//允许的跨域请求方法。
            "AllowSites": [ "http://localhost" ],	//允许跨域请求的地址列表。
            "SupportsCredentials": true	//请求是否携带cookie、session等信息。
        }
    },

    "Cache": {
        "Default": {
            "Type": "Local",	//缓存类型，框架默认支持Local、Redis两种，可自定义扩展。
            "Default": true		//是否默认缓存，多个缓存只能有一个默认缓存，最后配置的有效。
        }
    }
}
```



## 如何使用

配置配置内容大部分是针对框架的属性，配置后，框架会自动感知并进行设置；但有些用户自定义的业务配置，用户会希望在代码中进行读取，即使是框架的属性配置，用户也有可能需要在代码中进行读取作为业务判断的一种逻辑；因此，框架提供了多种方式读取配置的内容。

##### 通过全局对象获取配置

此种方式允许用户在任何位置进行使用，不受代码位置显示。

```c#
if (Application.Instance().Compression.Enable)	//判断框架是否开启了结果压缩机制
{
    //TODO
}
```

##### 通过控制器的上下文对象获取配置

在控制器的上下文对象ControllerContext中，我们可以通过Config属性获取配置内容。

```c#
public async Task GetList(ControllerContext cc)
{
	if (cc.Config.Compression.Enable)	//判断框架是否开启了结果压缩机制
	{
    	//TODO
	}
}
```

##### 自定义配置的获取

框架的配置都有对应的配置属性进行读取，对于用户自定义的配置，全部放在Settings动态对象中，用户可以通过该对象进行获取，事实上，框架的配置属性也全部定义在该对象内。

```c#
if (Application.Instance().Settings.Compression.Enable)
{
	//TODO
}
```



用户自定义配置：

```jSon
{
    "AllowedHosts": "*",	//允许访问的主机地址，默认为*，即无限制。
    ...
    "WechatSecret": "owejksfdpi23"	//用户自定义业务配置。
}
```

读取方式：

```c#
string wechatSecret = Application.Instance().Settings.WechatSecret;
```

