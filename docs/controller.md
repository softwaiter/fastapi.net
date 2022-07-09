# 控制器（Controller）



## 什么是Controller

[前面章节](router.md)写到，我们通过 Router 将用户的请求基于 method 和 URL 分发到了对应的 Controller 上，那 Controller 负责做什么？

简单的说 Controller 负责解析用户的输入，处理后返回相应的结果，例如

* 在 RESTful接口中，Controller 接受用户的参数，从数据库中查找内容返回给用户或者将用户的请求更新到数据库中。
* 在 HTML 页面请求中，Controller 根据用户访问不同的 URL，渲染不同的模板得到 HTML 返回给用户。
* 在代理服务器中，Controller 将用户的请求转发到其他服务器上，并将其他服务器的处理结果返回给用户。

框架推荐 Controller 层主要对用户的请求参数进行处理（校验、转换），然后调用对应的 Service 方法处理业务，得到业务结果后封装并返回：

1. 获取用户通过 HTTP 传递过来的请求参数。
2. 校验、组装参数。
3. 调用 Service 进行业务处理，必要时处理转换 Service 的返回结果，让它适应用户的需求。
4. 通过 HTTP 将结果响应给用户。



## 如何编写Controller

Controller是一个实现了特定方法的普通类，使用时根据类全名称+方法名进行调用；类文件建议放在Controllers目录下（也可根据实际需要放在项目下的其他目录中）。

Controller的方法必须遵循如下规则：
* 方法需要定义为一个返回Task的异步方法
* 方法有且只有一个[ControllerContext](objects.md)类型的参数

定义时建议继承BaseController基类，通过继承BaseController类，可以在Controller中方便的获取App全局对象和Service服务实例。

### 1. 常规Controller编写方法
假设我们实现一个用户登录的接口服务，需要首先实现如下Controller类

```c#
public class LoginController : BaseController
{
	public async Task LoginByAccount(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("登录成功");
    }
}
```

然后，将上面编写好的Controller在路由定义文件中进行配置

```xml
<?xml version="1.0" encoding="utf-8" ?>
<routers>
	<router path="/login" method="POST" handler="CodeM.FastApi.Controllers.LoginController.LoginByAccount" />
</routers>
```

至此，我们已经完成了一个接口服务的开发，当系统正常运行时，我们可以通过POST方法访问/login路由进行用户的登录请求。

**注**：定义的Controller类在有HTTP请求发生时，会实例化一个对象对请求进行响应；而示例中的Controller继承于<font color="#d56161">```BaseController```</font>，会有如下的this属性和方法：

<font color="#d56161">```this.App```</font>：当前应用[App](objects.md)对象的实例，通过它我们可以拿到框架提供的全局对象和方法。

<font color="#d56161">```this.Service(bool singleton = true)```</font>：该方法会返回一个和当前Controller同名的Service对象的实例，singleton参数用于指定是否使用单例模式；如：CodeM.FastApi.Controller.LoginController中调用该方法，系统会找到CodeM.FastApi.Service.LoginService对象，然后实例化后返回；要注意命名空间前缀的一致性。

<font color="#d56161">```this.Service(string serviceName, bool singleton = true)```</font>：该方法和上面的方法相似，都是获取Service对象实例的方法，该方法可以指定要获取Service对象的名称；如：CodeM.FastApi.Controller.LoginController中调用```this.Service("User", true)```方法，系统会找到CodeM.FastApi.Service.UserService对象，然后实例化后返回；要注意命名空间前缀的一致性。

### 2. Restful风格Controller编写方法
Restful风格的Controller编写方法是通过在定义类中实现指定名称的标准方法，达到对特定操作对象进行增删改查的目的。
假设我们要实现一套对Person人员进行操作的Restful接口：

```c#
public PersonController : BaseController
{
    public async Task Create(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("创建Person。");
    }

    public async Task Delete(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("删除Person。");
    }

    public async Task Update(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("修改Person。");
    }

    public async Task List(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("查询Person列表。");
    }

    public async Task Detail(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync("查询Person详情。");
    }
}
```

然后，将上面编写好的Controller在路由定义文件中进行配置

```xml
<?xml version="1.0" encoding="utf-8" ?>
<routers>
	<router path="/person" resource="CodeM.FastApi.Controllers.PersonController" />
</routers>
```

通过以上的代码和配置，我们已经成功为Person对象实现了增、删、改、查的接口服务：
<font color="#d56161">`POST /person`</font>	// 新建Person对象
<font color="#d56161">`DELETE /person/{id}`</font>	// 删除Person对象
<font color="#d56161">`PUT /person/{id}`</font>	// 修改Person对象
<font color="#d56161">`GET /person`</font>	// 查询Person对象列表
<font color="#d56161">`GET /person/{id}`</font>	// 查询Person对象详情



## HTTP 基础

由于 Controller 基本上是业务开发中唯一和 HTTP 协议打交道的地方，在继续往下了解之前，我们首先简单的看一下 HTTP 协议是怎样的。

如果我们发起一个 HTTP 请求来访问前面例子中提到的 Controller：

```bash
curl -X POST http://localhost:5000/person --data '{"name":"张三", "age": 18}' --header 'Content-Type:application/json; charset=UTF-8'
```

通过 curl 发出的 HTTP 请求的内容就会是下面这样的：

```http
POST /person HTTP/1.1 Host: localhost:5000 Content-Type: application/json; charset=UTF-8
{"name": "张三", "age": 18}
```

请求的第一行包含了三个信息，我们比较常用的是前面两个：

- method：这个请求中 method 的值是 `POST`。
- path：值为 `/person`，如果用户的请求中包含 query，也会在这里出现

从第二行开始直到遇到的第一个空行位置，都是请求的 Headers 部分，这一部分中有许多常用的属性，包括这里看到的 Host，Content-Type，还有 `Cookie`，`User-Agent` 等等。在这个请求中有两个头：

- <font color="#d56161">`Host`</font>：我们在浏览器发起请求的时候，域名会用来通过 DNS 解析找到服务的 IP 地址，但是浏览器也会将域名和端口号放在 Host 头中一并发送给服务端。
- <font color="#d56161">`Content-Type`</font>：当我们的请求有 body 的时候，都会有 Content-Type 来标明我们的请求体是什么格式的。

之后的内容全部都是请求的 body，当请求是 POST, PUT, DELETE 等方法的时候，可以带上请求体，服务端会根据 Content-Type 来解析请求体。

在服务端处理完这个请求后，会发送一个 HTTP 响应给客户端

```http
HTTP/1.1 200 OK Content-Type: application/json; charset=utf-8 Content-Length: 42 Date: Tue, 19 Apr 2022 01:40:12 GMT Connection: keep-alive
{"code":0,"data":"创建Person。","error":null}
```

第一行中也包含了三段，其中我们常用的主要是[响应状态码](https://baike.baidu.com/item/HTTP%E7%8A%B6%E6%80%81%E7%A0%81)，这个例子中它的值是 200，它的含义是请求已经被成功接收并返回。

和请求一样，从第二行开始到下一个空行之间都是响应头，这里的 Content-Type，表示这个响应的格式是 JSON；Content-Length，表示响应内容长度为 42 个字节。

最后剩下的部分就是这次响应真正的内容。



## 获取 HTTP 请求参数

 从上面的 HTTP 请求示例中可以看到，有好多地方可以放用户的请求数据，框架通过在 Controller 上绑定的 [ControllerContext](objects.md) 实例，提供了许多便捷方法和属性获取用户通过 HTTP 请求发送过来的参数。 

### Query

 在 URL 中 <font color="#d56161">`?`</font> 后面的部分是一个 Query String，这一部分经常用于 GET 类型的请求中传递参数。例如 <font color="#d56161">`GET /person?pageindex=1&pagesize=50`</font> 中 <font color="#d56161">`pageindex=1&pagesize=50`</font> 就是用户传递过来的参数。我们可以通过 <font color="#d56161">`ControllerContext.QueryParams`</font> 拿到解析过后的这个参数体 。

```c#
public PersonController : BaseController
{
    public async Task List(ControllerContext cc)
    {
        string pageindex = cc.QueryParams["pageindex"];
        string pagesize = cc.QueryParams["pagesize"];
        
        // TODO
        await cc.JsonAsync("查询Person列表。");
    }
}
```

当 Query String 中的 key 重复时，使用上面的方法只取 key 第一次出现时的值，后面再出现的都会被忽略。<font color="#d56161">`GET /person?name=张三&name=李四`</font> 拿到的值是 <font color="#d56161">`张三`</font>。

##### QueryParams对象提供了更多的方法和属性对Query参数进行操作：

###### 属性

Count：返回Query参数的个数，同名参数不重复计数。

###### 方法

bool ContainsKey(string key)：判断是否包含指定key的Query参数。

string Get(string key, string defaultValue)：返回指定key的Query参数值，有多个同名key参数时，返回第一个；如果不存在，则返回defaultValue。

string Get(int index, string defaultValue)：返回指定index的Query参数值；如果不存在，则返回defaultValue。

<a id="link_allvalues">StringValues AllValues(string key)</a>：返回指定key的Query参数值，有多个同名key参数时，返回所有同名参数值；如果不存在，则返回null。

StringValues AllValues(int index)：返回指定index的Query参数值；指定索引的参数包含多个值时，返回所有参数值； 如果不存在，则返回null。

### Queries

有时候我们的系统会设计成让用户传递相同的 key，例如 <font color="#d56161">`DELETE /person?id=1&id=2&id=3`</font>。针对此类情况，框架在<font color="#d56161">`QueryParams` </font>对象上实现了[AllValues](#link_allvalues)方法，通过这个方法，可以获取到指定key的所有参数值，它不会丢弃任何一个重复的数据，而是将他们都放到一个集合中：

```c#
// DELETE /person?id=1&id=2&id=3
public class PersonController : BaseController
{
    public async Task Delete(ControllerContext cc)
    {
        StringValues ids = cc.QueryParams.AllValues("id");
        await cc.JsonAsync(string.Format("删除了{0}个Person。", ids.Count));
    }    
}
```

### Router Params

在 [Router](router.md) 中，我们介绍了 Router 上也可以申明路由参数，这些参数都可以通过 `ControllerContext`对象上`RouteParams`属性 获取到。 

```c#
// DELETE /person/{id}
// DELETE /person/1
public class PersonController : BaseController
{
    public async Task Delete(ControllerContext cc)
    {
        string id = cc.RouteParams["id"];
        await cc.JsonAsync(string.Format("删除的Person是{0}。", id));
    }    
}
```

### body（Form）

虽然我们可以通过 URL 传递参数，但是还是有诸多限制：

- [浏览器中会对 URL 的长度有所限制](http://stackoverflow.com/questions/417142/what-is-the-maximum-length-of-a-url-in-different-browsers)，如果需要传递的参数过多就会无法传递。

- 服务端经常会将访问的完整 URL 记录到日志文件中，有一些敏感数据通过 URL 传递会不安全。

当请求的ContentType为`application/x-www-form-urlencoded` ，表示请求是通过Key/Value的表单形式进行参数传递，这些参数可以通过`ControllerContext`对象上`PostForms`属性 获取到。

```c#
// POST /person
public class PersonController : BaseController
{
    public async Task Create(ControllerContext cc)
    {
        string name = cc.PostForms["name"];
  		int age = int.Parse(cc.PostForms["age"]);
        // TODO 保存人员信息到数据库
        await cc.JsonAsync("创建Person成功。");
    }
}
```

### body（Json）

提交数据除了采用上面介绍的Form表单传参形式外，最常用的就是使用Json格式进行参数传递；相较于Form的Key/Value格式，Json能够提供更丰富的数据类型，支持更复杂的数据结构；当用户使用Json格式进行参数传递时，可以通过`ControllerContext`对象上`PostJson`属性 获取到。

上面新建Person的Controller假如使用Json格式传递参数的话，代码将如下面所示：

```c#
// POST /person
public class PersonController : BaseController
{
    public async Task Create(ControllerContext cc)
    {
        string name = cc.PostJson.name;
  		int age = cc.PostJson.age;
        // TODO 保存人员信息到数据库
        await cc.JsonAsync("创建Person成功。");
    }
}
```

### 获取上传的文件

当面对需要进行文件上传的业务场景时，请使用Asp.netcore系统功能，从Request.Form.Files获取上传文件的列表进行操作，通过Request.Form.Files对象可以获取上传文件的数量和具体内容。

```c#
// POST /person/head
public class PersonController : BaseController
{
    public async Task UploadHead(ControllerContext cc)
    {
        if (cc.Request.Form != null &&
           cc.Request.Form.Files != null)
        {
            if (cc.Request.Form.Files.Count == 1)
            {
                // TODO 更新用户头像
                await cc.JsonAsync("更新用户头像成功。");
            }
        }
        
        await cc.JsonAsync(-1, "修改用户头像失败。");
    }
}
```

上传文件的大小限制默认最大5M，可以通过在appsettings.json中配置FileUpload相关属性进行修改。

```js
{
    "FileUpload": {
        "MaxBodySize": 5242880	// 单位：字节
    }
}
```

### Header

除了从 URL 和请求 body 上获取参数之外，还有许多参数是通过请求 header 传递的。 控制器方法中的唯一参数`ControllerContext`对象上提供了`Headers`属性可以获取到header的所有信息。

````c#
// POST /person/head
public class PersonController : BaseController
{
    public async Task Create(ControllerContext cc)
    {
        string token = cc.Headers.Get("token", null);
        if (token == null)
        {
        	await cc.JsonAsync(-1, "缺少请求签名。");    
            return;
        }
        
        // TODO
        await cc.JsonAsync("创建Person成功。");
    }
}
````

### Cookie

HTTP 请求都是无状态的，但是我们的 Web 应用通常都需要知道发起请求的人是谁。为了解决这个问题，HTTP 协议设计了一个特殊的请求头：[Cookie](https://baike.baidu.com/item/cookie/1119)。服务端可以通过响应头（set-cookie）将少量数据响应给客户端，浏览器会遵循协议将数据保存，并在下次请求同一个服务的时候带上（浏览器也会遵循协议，只在访问符合 Cookie 指定规则的网站时带上对应的 Cookie 来保证安全性）。 

 通过 `ControllerContext.Cookies`，我们可以在 Controller 中便捷、安全的设置和读取 Cookie。 

```c#
// POST  /login
public class LoginController : BaseController
{
    public async Task Login(ControllerContext cc)
    {
        string token = Guid.NewGuid().ToString("N");
        
        // TODO
        
        CookieOptionsExt coe = new CookieOptionsExt();
        coe.Encrypt = true;
        coe.HttpOnly = true;
        cc.Cookies.Set("token", token, coe);
        
        await cc.JsonAsync("登录成功。");
    }
}
```

Cookie 虽然在 HTTP 中只是一个头，但是通过 `foo=bar;foo1=bar1;` 的格式可以设置多个键值对。

Cookie 在 Web 应用中经常承担了传递客户端身份信息的作用，因此有许多安全相关的配置，不可忽视，[Config配置](config.md) 章节中详细介绍了 Cookie 的安全相关的配置项，可以深入阅读了解。

### Session

通过 Cookie，我们可以给每一个用户设置一个 Session，用来存储用户身份相关的信息，这份信息会加密后存储在 Cookie 中，实现跨请求的用户身份保持。

框架给我们提供了 `ControllerContext.Session` 来访问或者修改当前用户 Session 。 

```c#
// POST  /login
public class LoginController : BaseController
{
    public async Task Login(ControllerContext cc)
    {
        string user = cc.PostJson.user;
        cc.Session.SetString("username", user);

        // TODO
        
        await cc.JsonAsync("登录成功。");
    }
}
```

 Session 的使用方法非常直观，直接读取或者修改指定key值就可以了，如果要删除key，直接将它Remove： 

```c#
// POST  /logout
public class LoginController : BaseController
{
    public async Task Logout(ControllerContext cc)
    {
        cc.Session.Remove("username");

        // TODO
        
        await cc.JsonAsync("注销登录成功。");
    }
}
```

如果想正常使用Session，必须首先在配置文件中开启Session；否则，使用时将收到异常信息。

```js
{
    "Session": {
        "Enable": true
    }
}
```

除此之外，Session 还有许多其他配置选项，在使用之前请详细阅读[Config配置](config.md) Session相关的章节。 



## 调用Service

我们并不想在 Controller 中实现太多业务逻辑，所以提供了一个 [Service](service.md) 层进行业务逻辑的封装，这不仅能提高代码的复用性，同时可以让我们的业务逻辑更好测试。

在 Controller 中可以调用任何一个 Service 上的任何方法，框架在BaseController基类上定义了如下方法：

###### public dynamic Service(string serviceName, bool singleton = true)

`serviceName`必须是Service定义类名去除Service后缀的部分，且Service定义类必须和Controller定义类命名空间保持父级的一致性。如下：

```c#
// Controller 定义
namespace MyProgram.Controllers
{
    // POST /person
    public class PersonController : BaseController
    {
        public async Task Create(ControllerContext cc)
        {
            string name = cc.PostJson.name;
            int age = cc.PostJson.age;

            Service("Person").Save(name, age);
            
            // 当Service类名和Controller类型前缀一致时，可省略serviceName参数
            // 如本例中的PersonService和PersonController
            // Service().Save(name, age);

            await cc.JsonAsync("创建Person成功。");
        }
    }
}

// Service 定义
namespace MyProgram.Services // 命名空间父级一致，都是MyProgram，可以是多级，一致即可
{
    public class PersonService : BaseService
    {
        public bool Save(string name, int age)
        {
            // TODO
            return true;
        }
    }
}
```

通过`singleton`参数可以控制是否使用单例模式调用Service对象；默认为true，使用单例模式。

Service 更多的具体写法，请查看 [Service](service.md) 章节。 



## 发送HTTP响应

当业务逻辑完成之后，Controller 的最后一个职责就是将业务逻辑的处理结果通过 HTTP 响应发送给用户。 



### 设置 status

HTTP 设计了非常多的[状态码](https://baike.baidu.com/item/HTTP%E7%8A%B6%E6%80%81%E7%A0%81) ，每一个状态码都代表了一个特定的含义，通过设置正确的状态码，可以让响应更符合语义。 

框架在`ControllerContext`对象上提供了一个`State`属性便捷的进行状态码的设置

```c#
// POST /person
public class PersonController : BaseController
{
    public async Task Create(ControllerContext cc)
    {
        // TODO
        cc.State = 200;	// 200是成功状态码，是所有Http请求成功后的正常返回，通常可以省略不设置。

        await cc.JsonAsync("创建Person成功。");
    }
}
```

### 设置 body

绝大多数的数据都是通过 body 发送给请求方的，和请求中的 body 一样，在响应中发送的 body，也需要有配套的 Content-Type 告知客户端如何对数据进行解析。 

- 作为一个 RESTful 的 API 接口 controller，我们通常会返回 Content-Type 为 `application/json` 格式的 body，内容是一个 JSON 字符串。

框架在`ControllerContext`对象上提供了两个方法设置Json内容，使用框架方法将返回统一格式的Json字符串，具体格式如下：

```js
{
    code: 0,	// 返回码，0代表成功，-1代表失败；也可以自定义其他错误码。
    data: null,	// 返回数据，根据不同接口逻辑，可以返回列表、对象等任意类型数据；也可以是null。
    error: null	// 当返回码不为0时，显示失败的文字描述或错误信息。
}
```

##### 方法一：直接设置返回数据，方法根据数据类型自动设置code返回码。

###### public static async Task JsonAsync([object _data = null])

使用该方法设置返回JSON内容时，只能对返回数据或错误信息一项内容进行赋值。

当\_data为null或者非Exception类型的数据时，code码将设置为0，\_data作为返回数据使用。

```c#
// POST /person
public class PersonController : BaseController
{
    public async Task Detail(ControllerContext cc)
    {
        // TODO

        object detailObj = new
        {
            name = "wangxm",
            age = 18
        };
        await cc.JsonAsync(detailObj);        
    }
}
```

```js
// 返回结果
{
    "code":0,
    "data":{
        "name":"wangxm",
        "age":18
    },
    "error":null
}
```

当\_data是一个Exception类型的对象时，code码将设置为-1，\_data作为错误信息使用

```c#
// POST /person
public class PersonController : BaseController
{
    public async Task Detail(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync(new Exception("查询Person详情异常。"));
    }
}
```

```js
// 返回结果
{
    "code":-1,
    "data":null,
    "error":"查询Person详情异常。"
}
```

##### 方法二：自定义返回码、返回数据以及错误信息

###### public static async Task JsonAsync(int _code, [object _data = null], [string _error = null])

通过该方法，可以更灵活的设置错误返回码；并同时设置返回数据和错误信息。

```c#
// POST  /login
public class LoginController : BaseController
{
    public async Task Login(ControllerContext cc)
    {
        // TODO
        await cc.JsonAsync(1001, null, "用户名或密码错误。");
    }
}
```

除了以上框架提供的JSON返回方法，你可以随时使用ASP.NETCORE自带的Response对象自定义返回的数据格式内容，使用方法遵循ASP.NETCORE相关说明。

