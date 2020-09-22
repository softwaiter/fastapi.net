using CodeM.Common.Ioc;
using CodeM.Common.Orm;
using CodeM.FastApi.Common;
using CodeM.FastApi.Config;
using CodeM.FastApi.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        private async Task _RequestHandlerAsync(HttpContext context,
            RouterConfig.RouterItem item, string handlerName)
        {
            ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);

            try
            {
                Stack<string> _responseMiddlewares = new Stack<string>();

                List<string> middlewares = new List<string>();
                middlewares.AddRange(mAppConfig.Middlewares);
                middlewares.AddRange(item.Middlewares);
                foreach (string middleware in middlewares)
                {
                    string middlewareMethod = string.Concat(middleware, ".Request");
                    mMethodInvoker.Invoke(middlewareMethod, cc,
                        item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance, true);

                    _responseMiddlewares.Push(middleware);

                    if (cc.RequestBreaked)
                    {
                        break;
                    }
                }

                if (!cc.RequestBreaked)
                {
                    mMethodInvoker.Invoke(handlerName, cc,
                        item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance);
                }

                while (_responseMiddlewares.Count > 0)
                {
                    string middleware = _responseMiddlewares.Pop();
                    string middlewareMethod = string.Concat(middleware, ".Response");
                    mMethodInvoker.Invoke(middlewareMethod, cc,
                        item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance, true);
                }
            }
            catch (Exception exp)
            {
                if (Utils.IsDevelopment())
                {
                    cc.State = 500;
                    await cc.Response.WriteAsync(exp.ToString(), Encoding.UTF8);
                }
                else
                {
                    throw exp;
                }
            }
        }

        private void _MountSingleHandler(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string method = !string.IsNullOrWhiteSpace(item.Method) ? item.Method.ToUpper() : "GET";

            if ("GET".Equals(method))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await _RequestHandlerAsync(context, item, item.Handler);
                });
            }
            else if ("POST".Equals(method))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await _RequestHandlerAsync(context, item, item.Handler);
                });
            }
            else if ("PUT".Equals(method))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await _RequestHandlerAsync(context, item, item.Handler);
                });
            }
            else if ("DELETE".Equals(method))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await _RequestHandlerAsync(context, item, item.Handler);
                });
            }
        }

        private void _MountResourceRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            object resouceInst = IocUtils.GetObject(item.Resource);

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
            if (Utils.IsMethodExists(resouceInst, "Create"))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Create");
                    await _RequestHandlerAsync(context, item, handlerFullName);
                });
            }

            //删
            if (Utils.IsMethodExists(resouceInst, "Delete"))
            {
                builder.MapDelete(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Delete");
                    await _RequestHandlerAsync(context, item, handlerFullName);
                });
            }

            //改
            if (Utils.IsMethodExists(resouceInst, "Update"))
            {
                builder.MapPut(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Update");
                    await _RequestHandlerAsync(context, item, handlerFullName);
                });
            }

            //查（列表）
            if (Utils.IsMethodExists(resouceInst, "List"))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".List");
                    await _RequestHandlerAsync(context, item, handlerFullName);
                });
            }

            //查（个体）
            if (Utils.IsMethodExists(resouceInst, "Detail"))
            {
                builder.MapGet(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Detail");
                    await _RequestHandlerAsync(context, item, handlerFullName);
                });
            }
        }

        private string _GetParamValue(ControllerContext cc, string name)
        {
            if (cc.PostJson != null && cc.PostJson.Has(name))
            {
                object result;
                if (cc.PostJson.TryGetValue(name, out result))
                {
                    return "" + result;
                }
            }

            if (cc.PostForms != null && cc.PostForms.ContainsKey(name))
            {
                return cc.PostForms[name];
            }

            return cc.QueryParams[name];
        }

        private async Task _CreateAndSaveModel(ControllerContext cc, RouterConfig.RouterItem item)
        {
            try
            {
                Model m = OrmUtils.Model(item.Model);
                dynamic obj = m.NewObject();

                int count = m.PropertyCount;
                for (int i = 0; i < count; i++)
                {
                    Property p = m.GetProperty(i);
                    if (!p.AutoIncrement)
                    {
                        string v = _GetParamValue(cc, p.Name);
                        if (v != null)
                        {
                            obj.SetValue(p.Name, v);
                        }
                    }
                }

                m.SetValues(obj).Save(true);

                await cc.JsonAsync();
            }
            catch (Exception exp)
            {
                await cc.JsonAsync(exp);
            }
        }

        private Regex mReOP = new Regex(">=|<=|<>|!=|>|<|=");
        //TODO  Like NotLike IsNull IsNotNull  In  NotIn

        private async Task _QueryModelList(ControllerContext cc, RouterConfig.RouterItem item)
        {
            int pagesize = 100;
            int.TryParse(cc.QueryParams.Get("pagesize", "100"), out pagesize);

            int pageindex = 1;
            int.TryParse(cc.QueryParams.Get("pageindex", "1"), out pageindex);

            SubFilter filter = new SubFilter();
            string where = cc.QueryParams.Get("where", null);
            if (!string.IsNullOrEmpty(where))
            {
                string[] subWheres = where.Split(",");
                foreach (string expr in subWheres)
                {
                    Match mc= mReOP.Match(expr);
                    if (mc.Success)
                    {
                        string name = expr.Substring(0, mc.Index);
                        string value = expr.Substring(mc.Index + mc.Length);
                        switch (mc.Value)
                        {
                            case ">=":
                                filter.Gte(name, value);
                                break;
                            case "<=":
                                filter.Lte(name, value);
                                break;
                            case "<>":
                            case "!=":
                                filter.NotEquals(name, value);
                                break;
                            case ">":
                                filter.Gt(name, value);
                                break;
                            case "<":
                                filter.Lt(name, value);
                                break;
                            case "=":
                                filter.Equals(name, value);
                                break;
                        }
                    }
                    else
                    {
                        throw new Exception(string.Concat("不识别的查询条件：where=", expr));
                    }
                }
            }

            Model m = OrmUtils.Model(item.Model);

            long total = -1;

            bool getTotal = false;
            bool.TryParse(cc.QueryParams.Get("gettotal", "false"), out getTotal);
            if (getTotal)
            {
                total = m.And(filter).Count();
            }

            string sort = cc.QueryParams.Get("sort", null);
            if (!string.IsNullOrEmpty(sort))
            {
                string[] subSorts = sort.Split(",");
                foreach (string ss in subSorts)
                {
                    string sItem = ss.ToLower();
                    if (sItem.EndsWith("_desc"))
                    {
                        m.DescendingSort(sItem.Substring(0, sItem.Length - 5));
                    }
                    else if (sItem.EndsWith("_asc"))
                    {
                        m.AscendingSort(sItem.Substring(0, sItem.Length - 4));
                    }
                    else
                    {
                        m.AscendingSort(sItem);
                    }
                }
            }
            
            List<dynamic> result = m.And(filter).PageSize(pagesize).PageIndex(pageindex).Query();
            await cc.JsonAsync(new
            {
                result = result,
                total = total
            });
        }

        private async Task _QueryModelDetail(ControllerContext cc, RouterConfig.RouterItem item)
        {
            Model m = OrmUtils.Model(item.Model);

            string id = cc.RouteParams["id"];
            string[] ids = id.Split("|");
            if (ids.Length != m.PrimaryKeyCount)
            {
                throw new Exception("无效的参数。");
            }

            for (int i = 0; i < m.PrimaryKeyCount; i++)
            {
                Property p = m.GetPrimaryKey(i);
                m.Equals(p.Name, ids[i]);
            }

            object detailObj = null;

            List<dynamic> result = m.Top(1).Query();
            if (result.Count > 0)
            {
                detailObj = result[0];
            }

            await cc.JsonAsync(detailObj);
        }

        private void _MountModelRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            //TODO
            string individualPath = item.Path;
            if (individualPath.EndsWith("/"))
            {
                individualPath += "{id}";
            }
            else
            {
                individualPath += "/{id}";
            }

            //新建
            builder.MapPost(item.Path, async (context) =>
            {
                ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);
                try
                {
                    await _CreateAndSaveModel(cc, item);
                }
                catch (Exception exp)
                {
                    if (Utils.IsDevelopment())
                    {
                        cc.State = 500;
                        await cc.Response.WriteAsync(exp.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        throw exp;
                    }
                }
            });

            //查列表
            builder.MapGet(item.Path, async (context) =>
            {
                ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);
                try
                {
                    await _QueryModelList(cc, item);
                }
                catch (Exception exp)
                {
                    if (Utils.IsDevelopment())
                    {
                        cc.State = 500;
                        await cc.Response.WriteAsync(exp.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        throw exp;
                    }
                }
            });

            //查详情
            builder.MapGet(individualPath, async (context) =>
            {
                ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);
                try
                {
                    await _QueryModelDetail(cc, item);
                }
                catch (Exception exp)
                {
                    if (Utils.IsDevelopment())
                    {
                        cc.State = 500;
                        await cc.Response.WriteAsync(exp.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        throw exp;
                    }
                }
            });

            //builder.MapDelete(individualPath, async (context) =>
            //{

            //});
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
