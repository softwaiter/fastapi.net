# 会话（Session）

大家都知道，在Web应用中，接口服务和浏览器页面之间的通信通常是基于HTTP协议的，而HTTP协议是无状态的，也就是各个请求之间是相互独立的。但一个真正的Web应用是解决某种具体业务需求的，而业务需求之间一定是有各种各样的依赖关系，需要通过某种状态进行关联的。如：一个人浏览电商网站，将某件商品放入购物车，进入购物车进行结算操作；在这里，将商品放入购物车，对购物车商品进行结算是两个独立的接口服务，但它们必须保持某种关系，整个购买操作才能正确完成，这种关系包括是谁选择了哪件商品放入了购物车，是谁进入购物车做了结算操作；而Session就是为了解决这种问题，Session是一种身份标识，通过这种身份，我们就可以在不同的接口服务中按照相同的身份进行业务关联和状态保持。



## 如何启用Session

在框架中，Session默认是启用状态；如果想修改Session启用状态，可以通过appsettings.json文件进行配置。

```json
"Session": {
	"Enable": false		//是否启用Session，默认为true，启用。
}
```



## Session配置说明

```json
"Session": {
    "Enable": true,
    "Timeout": "20m",
    "Cookie": {
        "Name": "fastapi.sid",
        "SameSite": "None",
        "HttpOnly": true,
        "MaxAge": "20m"
    }
}
```

Enable：是否启用Session，默认为true。

Timeout：session的空闲过期时间，即session没有使用的情况下能保持多长时间；默认20m，单位：ms-毫秒，s-秒，m-分钟，h-小时，d-天。

Cookie：浏览器端存储session会话cookie的相关设置。

​		Name：Cookie的名称，默认fastapi.sid。

​		SameSite：跨站请求时Cookie策略，默认Lax。

​		HttpOnly：是否禁止通过Javascript读取cookie，默认true。

​		MaxAge：Cookie有效期，默认未设置（有效期至浏览器关闭）；可根据需要设置相对过期时间，单位：ms-毫秒，s-秒，m-分钟，h-小时，d-天。



## Redis分布式Session

为了框架在分布式部署时对Session一致性的支持，用户可以选择使用Redis对Session进行存储，这样不同的服务节点可以保证使用相同的Session数据，避免Session不同引起业务混乱；默认Session是存在进程内存中，只有当前服务本身能进行访问；且进程重启后，Session数据将丢失；而Redis分布式Session能解决这些问题。

```json
"Session": {
    "Redis": {
        "Enable": false,
        "Host": "127.0.0.1",
        "Port": 6379,
        "Database": 0,
        "Password": "root",
        "Retry": 3,
        "Timeout": 5000,
        "Ssl": false,
        "SslHost": "",
        "SslProtocols": ""
    }
}
```

Enable：是否使用Redis存储Session，默认false。

Host：Redis服务的主机地址，默认127.0.0.1。

Port：Redis服务的端口号，默认6379。

Database：使用的Redis数据库，默认0。

Password：Redis服务的访问口令，默认空。

Retry：Redis服务连接的重试次数，默认3。

Timeout：Redis服务连接的超时时间，单位毫秒，默认5000。

Ssl：是否使用Ssl加密连接，默认false。

SslHost：Redis服务的Ssl访问主机地址，默认空。

SslProtocols：Ssl的安全协议版本，默认空。

​		None - 允许操作系统选择要使用的最佳协议，并将其用于阻止不安全的协议。 应使用此字段，除非应用有特定原因不得使用此字段。
​		Ssl2 - 指定 SSL 2.0 协议。 SSL 2.0 已由 TLS 协议取代，之所以仍然提供这个方法，只是为了向后兼容。
​		Ssl3 - 指定 SSL 3.0 协议。 SSL 3.0 已由 TLS 协议取代，之所以仍然提供这个方法，只是为了向后兼容。
​		Tls - 指定 TLS 1.0 安全协议。 提供 TLS 1.0 只是为了实现向后兼容性。 TLS 协议在 IETF RFC 2246 中定义。
​		Tls11 - 指定 TLS 1.1 安全协议。 TLS 协议在 IETF RFC 4346 中定义。
​		Tls12 - 指定 TLS 1.2 安全协议。 TLS 协议在 IETF RFC 5246 中定义。
​		Tls13 - 指定 TLS 1.3 安全协议。 此 TLS 协议在 IETF RFC 8446 定义。



## 代码中使用Session

在框架层面，用户可以全程获得ControllerContext对象，该对象下的Session对象 提供了方法可以方便的操作Session：

```c#
public async Task Handle(ControllerContext cc)
{
    cc.Session.SetString("userid", "wangxm");  //存储一条session信息
    await cc.JsonAsync("Hello World.");
}
```

- ControllerContext.Session.SetString(string key, string value) - 存储一条字符串Session信息。
- ControllerContext.Session.GetString(string key) - 获取Session指定key的Session信息，返回类型string。
- ControllerContext.Session.SetInt32(string key, Int32 value) - 存储一条32位整型的Session信息。
- ControllerContext.Session.GetInt32(string key) - 获取Session指定key的Session信息，返回了类型Int32？，允许null值。
- ControllerContext.Session.SetBoolean(string key, bool value) - 存储一条布尔型Session信息。
- ControllerContext.Session.GetBoolean(string key) - 获取Session指定key的Session信息，返回类型Boolean。