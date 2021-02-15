# 路由（Router）

Router主要用来指定接口路径和接口具体处理逻辑的Controller的映射关系，框架约定了在router.xml文件中统一定义所有路由的规则。

通过统一的配置，我们可以避免路由规则逻辑散落在多个地方，从而出现未知的冲突，集中在一起我们可以更方便的来查看全局的路由规则。



## 如何定义Router

router.xml里面定义接口规则

```xml
<!--router.xml-->
<routers>
    <router path="/user" handler="CodeM.FastApi.Controller.User.Handle"></router>
</routers>
```

controller目录下实现Controller（框架建议在controller目录下实现所有Controller，并不强制）

```c#
// controller/User.cs
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Handle(ControllerContext cc)
        {
            await cc.JsonAsync("Hello World.");
        }
    }
}
```

这样就完成了一个最简单的Router定义，当用户执行GET /user，User.cs里的的Handle方法就会执行。



## Router详细定义说明

下面是路由的完整定义，参数可以根据场景的不同，自由选择。

```xml
<router path="路径" method="方法" handler="处理器" resource="资源" model="数据模型" middlewares="中间件" maxConcurrent="最大并发" maxIdle="空闲数量" maxInvokePerInstance="每实例执行次数" include="子路由定义文件"></router>
```

路由定义可选参数主要包括10个部分：

* path - 接口路径，即通常所说的URL路径（必填）。
* method - 接口请求的类型，即HTTP对应的method方法，现支持GET、POST、PUT、DELETE，默认GET（可选）。
* handler - 指定路由具体执行业务逻辑的代码实现，需要填写完整代码路径，即命名空间+类名+方法名（可选）。
* resource - 框架支持restful风格的路由定义，通过指定一个类，快速定义一组CRUD路由，需要指定类全名称（可选）。
* model - 框架内置ORM能力，通过指定一个 model定义，快速定义一组针对指定model的CRUD路由（可选）。
* action - 配合model定义路由的接口行为，取值范围CURLD，对应增、改、删、列表、详情，可自由组合，默认CURLD。
* batchAction - 配合model定义路由的接口批操作行为，取值范围CUR，对应批量增、批量改、批量删，可自由组合，默认空。
* middlewares - 路由定义支持中间件设置，通过设置中间件可以在具体业务逻辑执行前或者执行后进行额外的处理（可选）。
* maxConcurrent - 路由请求最大并发数，超过最大数，直接返回状态繁忙，默认100（可选）。
* maxIdle - 指定路由空闲状态时，存活的最大实例数，默认10（可选）。
* maxInvokePerInstance - 指定路由实例最大使用次数，一旦达到规定数量，将销毁当前实例，并创建新的实例作为代替，默认10000（可选）。
* include - 支持包含子文件，避免所有路由定义在一个文件中繁杂，不容易管理。

### 注意

* router.xml必须在根目录下。
* router定义中，middleware中间件支持多个串联执行。
* handler、resource、model属性必须设置一个，不能同时使用。
* 使用resource、model后，不能再设置method属性。
* include指向的子文件是以路由定义文件所在目录为基准目录。

下面是一些路由的定义方式：

```xml
<router path="/user" handler="CodeM.FastApi.Controller.User.Handle"></router>
<router path="/user/{id}" handler="CodeM.FastApi.Controller.User.Detail"></router>
<router path="/user" method="POST" handler="CodeM.FastApi.Controller.User.Create"></router>
```



### RESTful 风格的 URL 定义

如果想通过RESTful的方式来定义路由，我们提供了两种方法可以轻松实现。

#### 1. 通过resource属性，指定一个类快速在一个路径上生成CRUD功能。

```xml
<router path="/user" resource="CodeM.FastApi.Controller.User"></router>
```

上面的配置就在/user路径上部署一组CRUD功能，对应的CRUD处理逻辑在resource指向的类中提供，用户只需要在类中实现对应的函数就可以了。

| Method | Path       | Router Name | Description |
| ------ | ---------- | ----------- | ----------- |
| POST   | /user      | Create      | 增          |
| DELETE | /user/{id} | Delete      | 删          |
| PUT    | /user/{id} | Update      | 改          |
| GET    | /user      | List        | 查列表      |
| GET    | /user/{id} | Detail      | 查详情      |

```c#
// controller/User.cs
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Create(ControllerContext cc)
        {
            await cc.JsonAsync("增");
        }
        
        public async Task Delete(ControllerContext cc)
        {
            await cc.JsonAsync("删");
        }
        
        public async Task Update(ControllerContext cc)
        {
            await cc.JsonAsync("改");
        }
        
        public async Task List(ControllerContext cc)
        {
            await cc.JsonAsync("查列表");
        }
        
        public async Task Detail(ControllerContext cc)
        {
            await cc.JsonAsync("查详情");
        }
    }
}
```

如果我们不需要其中的某几个方法，可以不用在User类里实现，这样对应的URL路径也不会注册到Router。



#### 2. 通过model属性，指定一个数据模型，自动生成针对该模型定义的CRUD功能。

框架自带了一套轻量级开源ORM库，ORM库使用方法和Model定义规则可参见[使用说明](http://www.github.com/softwaiter/netcoreORM)

通过针对指定Model配置一条路由，就可以即刻获得针对该Model的增、删、改、查的接口。



路由定义：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<routers>
    <router path="/user" model="User"></router>
</routers>
```

如上定义了一条/user的路由，该条路由绑定了用户模型User；针对该条路由定义框架会在运行时生成如下几条路由：

| 方法   | 路由       | 功能         | 参数                                                         |
| ------ | ---------- | ------------ | ------------------------------------------------------------ |
| POST   | /user      | 新建用户     | --                                                           |
| POST   | /user      | 批量新建用户 | 只支持json传参，格式如下：<br>{<br>    "_items": [<br>        {<br>            "Name":"wangxm"<br>        },<br>        {<br>            "Name":"wangxm2"<br>        }<br>    ]<br>} |
| GET    | /user      | 查询用户列表 | pagesize:  分页大小，默认为50；最大200。<br/><br>pageindex: 第几页，默认1。<br/><br>gettotal: 是否返回总数，默认false。<br/><br>sort: 排序方式，格式为排序“排序属性_排序方式”，排序方式分为ASC和DESC两种。<br>如按照用户名年龄降序排列：/user?sort=Age_desc；多个排序条件增加同名参数即可，如：/user?sort=Age_desc&sort=Name_asc<br><br>where: 查询条件，使用表达式进行定义，支持的操作符包括（、）、AND、OR、>=、<=、<>、>、<、=、~=、~!=；~=表示数据库的Like，~!=表示数据库的Not Like。<br>如查找用户名以王开头的用户：/user?where=Name~=王%<br><br>source: 返回的模型属性，默认返回所有属性；多个属性用,分隔。 |
| GET    | /user/{id} | 查询用户详情 | --                                                           |
| DELETE | /user/{id} | 删除用户     | --                                                           |
| DELETE | /user      | 批量删除用户 | model主键：要删除的model模型的主键值，多个主键值可分别设置；如：?id=1&id=2&id=3 |
| PUT    | /user/{id} | 修改用户详情 | --                                                           |
| PUT    | /user      | 批量修改用户 | model主键：要删除的model模型的主键值，多个主键值可分别设置；如：?id=1&id=2&id=3 |

配置action属性，可以定义生成哪些路由，假如只需要用户模型User的列表查询接口，可进行如下定义：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<routers>
    <router path="/user" model="User" action="L"></router>
</routers>
```




## router实战

下面通过更多实际的例子，来说明router的用法。

### 参数获取

#### Query String方式

```xml
<!--router.xml-->
<router path="/user" handler="CodeM.FastApi.Controller.User.Handle"></router>
```

```c#
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Handle(ControllerContext cc)
        {
            string id = cc.QueryParams["id"];
            await cc.JsonAsync("User id is: " + id);
        }
    }
}
```

```shell
curl http://127.0.0.1:5000/user?id=1
```



#### 路由参数方式

```xml
<!--router.xml-->
<router path="/user/{id}" handler="CodeM.FastApi.Controller.User.Handle"></router>
```

```c#
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Handle(ControllerContext cc)
        {
            string id = cc.RouteParams["id"];
            await cc.JsonAsync("User id is: " + id);
        }
    }
}
```

```shell
curl http://127.0.0.1:5000/user/1
```



在路由定义中使用路由参数时，可以对路由参数进行约束，支持类型约束、范围约束和正则表达式约束3中形式。

##### 类型约束

| 约束     | 格式          | 说明               |
| -------- | ------------- | ------------------ |
| int      | {id:int}      | 只允许int32整数    |
| alpha    | {id:alpha}    | 只能包含大小写字母 |
| bool     | {id:bool}     | 只允许布尔类型     |
| datetime | {id:datetime} | 只允许日期格式     |
| decimal  | {id:decimal}  | 只允许decimal类型  |
| double   | {id:double}   | 只允许double类型   |
| float    | {id:float}    | 只允许float类型    |
| guid     | {id:guid}     | 只允许guid类型     |

##### 范围约束

| 约束             | 格式               | 说明               |
| ---------------- | ------------------ | ------------------ |
| length(length)   | {id:length(12)}    | 字符串长度限制     |
| maxlength(value) | {id:maxlength(8)}  | 字符串最大长度限制 |
| minlength(value) | {id:minlength(4)}  | 字符串最小长度限制 |
| range(min,max)   | {id:range(18,120)} | 数值范围限制       |
| min(value)       | {id:min(18)}       | 最小数值限制       |
| max(value)       | {id:max(120)}      | 最大数值限制       |

##### 正则表达式约束

| 约束              | 格式                                                       | 说明           |
| ----------------- | ---------------------------------------------------------- | -------------- |
| regex(expression) | {id:regex(^[^@]{{1,}}@[^@\.]{{1,}}\.(com\|cn\|net\|org)$)} | 正则表达式约束 |

约束规则可在同一个路由定义中组合使用，如下：

```xml
<!--router.xml-->
<router path="/user/{id:alpah:length(3)}" handler="CodeM.FastApi.Controller.User.Handle"></router>
```



#### 表单参数方式

```xml
<!--router.xml-->
<router path="/user" method="POST" handler="CodeM.FastApi.Controller.User.Create"></router>
```

```c#
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Create(ControllerContext cc)
        {
            await cc.JsonAsync("User name is " + cc.PostForms["name"] + ", age is " + cc.PostForms["age"]);
        }
    }
}
```

```shell
curl -d "name=wangxm&age=18" http://127.0.0.1:5000/user
```



#### Post参数内容

Post提交除了form-data参数形式，还可能是json、text、xml等其他形式，这时可使用如下方式获得参数内容：

```xml
<!--router.xml-->
<router path="/user" method="POST" handler="CodeM.FastApi.Controller.User.Create"></router>
```

```c#
using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {
        public async Task Create(ControllerContext cc)
        {
            await cc.JsonAsync("User data is " + cc.PostContent);
        }
    }
}
```

```shell
curl -H "Content-Type: application/json" -X POST --data '{"name":"wangxm","age":18}' http://127.0.0.1:5000/user
```

##### 注：通过Post方式提交的任何参数都可通过PostContent得到，但PostContent仅提供字符串格式的结果；如需其他格式，如json，需要用户根据PostContent内容自行转换。



### 太多路由映射？

框架不建议将路由定义分散在太多地方，尽量定义在根目录的router.xml中，便于问题的排查和发现。

若确实有需求，可以拆分成子文件，在子文件内进行定义，最后通过include属性在router.xml中进行整合。

```xml
<!--/router.xml-->
<router include="/sub_router.xml"></router>
<router include="/sub_router2.xml"></router>
```

```xml
<!--/routers/sub_router.xml-->
<router path="/user" handler="CodeM.FastApi.Controller.User.Handle"></router>
```

```xml
<!--/routers/sub_router2.xml-->
<router path="/user" method="POST" handler="CodeM.FastApi.Controller.User.Create"></router>
```

##### 注：include属性指向的目录是以当前路由文件所在目录为参照的相对路径。