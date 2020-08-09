using CodeM.FastApi.Config;
using CodeM.FastApi.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeM.FastApi.Router
{
    public class RouterManager
    {
        private AppConfig mAppConfig;

        private RouterConfig mRouterConfig;
        private MethodInvoker mMethodInvoker;

        public static RouterManager Current { get; } = new RouterManager();

        private RouterManager()
        {
            mRouterConfig = new RouterConfig();
            mMethodInvoker = new MethodInvoker();
        }

        public void Init(AppConfig config, string routerFile)
        {
            mAppConfig = config;
            mRouterConfig.Load(config, routerFile);
        }

        private async Task<bool> _ThroughMiddlewares(ControllerContext cc, RouterConfig.RouterItem item)
        {
            List<string> middlewares = new List<string>();
            middlewares.AddRange(mAppConfig.Middlewares);
            middlewares.AddRange(item.Middlewares);

            if (middlewares.Count > 0)
            {
                foreach (string middleware in middlewares)
                {
                    string middlewareMethod = string.Concat(middleware, ".Process");
                    object result = await mMethodInvoker.InvokeAsync(middlewareMethod, cc, 
                        item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance);
                    if (result != null)
                    {
                        bool bRet;
                        if (!bool.TryParse(result.ToString(), out bRet))
                        {
                            bRet = false;
                        }

                        if (!bRet)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task _RequestHandler(HttpContext context,
            RouterConfig.RouterItem item, string handlerName)
        {
            ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);
            if (await _ThroughMiddlewares(cc, item))
            {
                await mMethodInvoker.InvokeAsync(handlerName, cc,
                    item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance);
            }
        }

        private void _MountSingleHandler(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string method = !string.IsNullOrWhiteSpace(item.Method) ? item.Method.ToUpper() : "GET";

            if ("GET".Equals(method))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await _RequestHandler(context, item, item.Handler);
                });
            }
            else if ("POST".Equals(method))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await _RequestHandler(context, item, item.Handler);
                });
            }
            else if ("PUT".Equals(method))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await _RequestHandler(context, item, item.Handler);
                });
            }
            else if ("DELETE".Equals(method))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await _RequestHandler(context, item, item.Handler);
                });
            }
        }

        private void _MountResourceRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string individualPath = item.Path;
            if (individualPath.EndsWith("/"))
            {
                individualPath += "{id}";
            }
            else
            {
                individualPath += "/{id}";
            }

            //增
            builder.MapPost(item.Path, async (context) =>
            {
                string handlerFullName = string.Concat(item.Resource, ".Create");
                await _RequestHandler(context, item, handlerFullName);
            });

            //删
            builder.MapDelete(individualPath, async (context) =>
            {
                string handlerFullName = string.Concat(item.Resource, ".Delete");
                await _RequestHandler(context, item, handlerFullName);
            });

            //改
            builder.MapPut(individualPath, async (context) =>
            {
                string handlerFullName = string.Concat(item.Resource, ".Update");
                await _RequestHandler(context, item, handlerFullName);
            });

            //查（列表）
            builder.MapGet(item.Path, async (context) =>
            {
                string handlerFullName = string.Concat(item.Resource, ".List");
                await _RequestHandler(context, item, handlerFullName);
            });

            //查（个体）
            builder.MapGet(individualPath, async (context) =>
            {
                string handlerFullName = string.Concat(item.Resource, ".Detail");
                await _RequestHandler(context, item, handlerFullName);
            });
        }

        private void _MountModelRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            //TODO
        }

        private void _MountRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(item.Model))
            {
                _MountModelRouters(item, builder);
            }
            else if (!string.IsNullOrWhiteSpace(item.Resource))
            {
                _MountResourceRouters(item, builder);
            }
            else
            {
                _MountSingleHandler(item, builder);
            }
        }

        public void MountRouters(IApplicationBuilder app)
        {
            RouteBuilder builder = new RouteBuilder(app);

            mRouterConfig.Items.ForEach(item =>
            {
                this._MountRouters(item, builder);
            });

            IRouter router = builder.Build();
            app.UseRouter(router);
        }
    }
}
