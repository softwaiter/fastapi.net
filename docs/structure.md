# 目录结构

本章我们将对框架的整体目录结构做一个简单的介绍，让大家了解框架的目录组成和相关约定规范，方便大家在后面的使用过程中能够更加高效和准确。

```
fastapi.net-project
|-- appsettings.Development.json（开发环境配置文件，可选）
|-- appsettings.json（默认配置文件）
|-- appsettings.Production.json（生产环境配置文件，可选）
|-- Program.cs（框架启动入口文件，必需）
|-- Startup.cs（框架加载初始化文件，必需）
|-- router.xml（路由定义文件，必需）
|-- schedule.xml（可选）
|-- Controllers（控制器目录）
|   |-- BaseController.cs（控制器基类，非必要勿动）
|   |-- DemoController.cs（用户自定义业务控制器）
|-- Services（业务逻辑目录）
|   |-- BaseService.cs（业务逻辑基类，非必要勿动）
|   |-- DemoService.cs（用户自定义业务逻辑处理类）
|-- models（数据库模型目录）
|   |-- .connection.xml（用户自定义数据连接配置）
|   |-- .upgrade.xml（用户自定义数据升级文件，可选）
|   |-- demo.model.xml（用户自定义业务数据模型）
|-- Schedules（可选）
    |-- DemoJob.cs（用户自定义定时任务）
|-- System（系统级代码，必需，非必要勿修改）
    |-- Core
        |-- App.cs（系统插件装配和初始化）
        |-- CacheLoader.cs（加载系统缓存配置并初始化）
    |-- Middlewares
        |-- CorsMiddleware.cs（跨域处理中间件）
```

如上，由框架约定的目录，用户可根据实际业务需要自定义内容：

- `router.xml` - 用于配置URL路由规则，具体参见[路由(Router)](router.md)。
- `schedule.xml` - 用于配置定时任务及其调度规则，可选，具体参见[定时任务](schedule.md)。
- `appsettings.json`、`appsettings.*.json` - 框架配置文件，具体参见[配置(Config)](config.md)。
- `Controlles/**` - 控制器实现类，解析用户输入，返回相应处理结果，具体参见[控制器(Controller)](controller.md)。
- `Services/**` - 用于放置业务逻辑实现类，可选，建议使用，具体参见[服务(Service)](service.md)。
- `models/.connection.xml` - 数据库连接配置文件，具体参见[数据库操作(ORM)](database.md)。
- `models/.upgrade.xml` - 数据库版本升级文件，具体参见[数据库操作(ORM)](database.md)。
- `models/*.model.xml` - 数据实体模型定义文件，具体参见[数据库操作(ORM)](database.md)。
- `Schedules/**` - 用于放置定时任务实现类，可选，具体参见[定时任务](schedule.md)。

