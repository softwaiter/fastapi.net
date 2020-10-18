using CodeM.Common.Ioc;
using CodeM.Common.Orm;
using CodeM.FastApi.Common;
using CodeM.FastApi.Config;
using CodeM.FastApi.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
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

        private delegate Task ControllerInvokeDelegate(ControllerContext cc, RouterConfig.RouterItem item, params object[] args);

        private long mAllHandlerCounter = 0;    //全部请求处理器计数，总阀门控制
        private async Task _ThroughRequestPipelineAsync(ControllerInvokeDelegate ControllerDelegate, 
            HttpContext context, RouterConfig.RouterItem item, params object[] args)
        {
            if (Interlocked.Increment(ref mAllHandlerCounter) <= mAppConfig.Router.MaxConcurrentTotal)
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
                            int.MaxValue, mAppConfig.Router.MaxIdlePerRouter,
                            mAppConfig.Router.MaxInvokePerInstance, true);

                        _responseMiddlewares.Push(middleware);

                        if (cc.RequestBreaked)
                        {
                            break;
                        }
                    }

                    if (!cc.RequestBreaked)
                    {
                        await ControllerDelegate(cc, item, args);
                    }

                    while (_responseMiddlewares.Count > 0)
                    {
                        string middleware = _responseMiddlewares.Pop();
                        string middlewareMethod = string.Concat(middleware, ".Response");
                        mMethodInvoker.Invoke(middlewareMethod, cc,
                            int.MaxValue, mAppConfig.Router.MaxIdlePerRouter,
                            mAppConfig.Router.MaxInvokePerInstance, true);
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
                finally
                {
                    Interlocked.Decrement(ref mAllHandlerCounter);
                }
            }
            else
            {
                Interlocked.Decrement(ref mAllHandlerCounter);
                throw new Exception("System busy.");
            }
        }


        private async Task _DefaultRouterHandlerAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            mMethodInvoker.Invoke(args[0] + "", cc,
                item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance);
            await Task.CompletedTask;
        }

        private void _MountSingleHandler(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string method = !string.IsNullOrWhiteSpace(item.Method) ? item.Method.ToUpper() : "GET";

            if ("GET".Equals(method))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync), 
                        context, item, item.Handler);
                });
            }
            else if ("POST".Equals(method))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
            else if ("PUT".Equals(method))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
            else if ("DELETE".Equals(method))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
        }

        #region Resource Router
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
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //删
            if (Utils.IsMethodExists(resouceInst, "Delete"))
            {
                builder.MapDelete(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Delete");
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //改
            if (Utils.IsMethodExists(resouceInst, "Update"))
            {
                builder.MapPut(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Update");
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //查（列表）
            if (Utils.IsMethodExists(resouceInst, "List"))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".List");
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //查（个体）
            if (Utils.IsMethodExists(resouceInst, "Detail"))
            {
                builder.MapGet(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Detail");
                    await _ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(_DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }
        }

        #endregion

        #region Model Router

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

        private async Task _CreateModelAsync(ControllerContext cc, 
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_CREATE");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

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
                finally
                {
                    mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                    {
                        return Math.Max(0, itemValue - 1);
                    });
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, ")"));
            }
        }

        private Regex mReOP = new Regex("\\(|\\)|\\s+AND\\s+|\\s+OR\\s+|>=|<=|<>|~!=|!=|~=|>|<|=", RegexOptions.IgnoreCase);
        //TODO  IsNull IsNotNull  In  NotIn

        private void BuildWhereFilter(IFilter filter, string op, string name, string value)
        {
            switch (op)
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
                case "~=":
                    filter.Like(name, value);
                    break;
                case "~!=":
                    filter.NotLike(name, value);
                    break;
            }
        }

        private IFilter ParseQueryWhereCondition(string where)
        {
            IFilter result = new SubFilter();

            if (!string.IsNullOrEmpty(where))
            {
                IFilter current = result;
                int offset = 0;
                string exprName = null;
                string exprValue;
                string aop = null;
                int bracket = 0;

                MatchCollection mc = mReOP.Matches(where);
                for (int i = 0; i < mc.Count; i++)
                {
                    Match m = mc[i];

                    string curOP = m.Value.Trim();
                    if ("AND".Equals(curOP, StringComparison.OrdinalIgnoreCase))
                    {
                        if (exprName != null)
                        {
                            exprValue = where.Substring(offset, m.Index - offset);

                            BuildWhereFilter(current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        offset = m.Index + m.Length;

                        if (bracket == 0 && current.Parent != null)
                        {
                            current = current.Parent;
                        }

                        SubFilter andFilter = new SubFilter();
                        current.And(andFilter);
                        current = andFilter;
                    }
                    else if ("OR".Equals(curOP, StringComparison.OrdinalIgnoreCase))
                    {
                        if (exprName != null)
                        {
                            exprValue = where.Substring(offset, m.Index - offset);

                            BuildWhereFilter(current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        offset = m.Index + m.Length;

                        if (bracket == 0 && current.Parent != null)
                        {
                            current = current.Parent;
                        }

                        SubFilter orFilter = new SubFilter();
                        current.Or(orFilter);
                        current = orFilter;
                    }
                    else if ("(".Equals(curOP))
                    {
                        bracket++;
                        offset = m.Index + m.Length;
                    }
                    else if (")".Equals(curOP))
                    {
                        if (exprName != null)
                        {
                            exprValue = where.Substring(offset, m.Index - offset);

                            BuildWhereFilter(current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        bracket--;
                        offset = m.Index + m.Length;

                        if (current.Parent != null)
                        {
                            current = current.Parent;
                        }
                    }
                    else
                    {
                        exprName = where.Substring(offset, m.Index - offset);
                        aop = curOP;
                        offset = m.Index + m.Length;

                        if (i == mc.Count - 1)
                        {
                            exprValue = where.Substring(offset).Trim();

                            BuildWhereFilter(current, aop, exprName, exprValue);
                            exprName = null;
                        }
                    }
                }
            }

            return result;
        }

        private ConcurrentDictionary<string, long> mModelRouterConcurrents = new ConcurrentDictionary<string, long>();
        private async Task _QueryModelListAsync(ControllerContext cc, 
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_LIST");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
                {
                    int pagesize = 100;
                    int.TryParse(cc.QueryParams.Get("pagesize", "100"), out pagesize);

                    int pageindex = 1;
                    int.TryParse(cc.QueryParams.Get("pageindex", "1"), out pageindex);

                    string where = cc.QueryParams.Get("where", null);
                    IFilter filter = ParseQueryWhereCondition(where);

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
                finally
                {
                    mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                    {
                        return Math.Max(0, itemValue - 1);
                    });
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, ")"));
            }
        }

        private async Task _QueryModelDetailAsync(ControllerContext cc, 
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_DETAIL");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
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
                finally
                {
                    mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                    {
                        return Math.Max(0, itemValue - 1);
                    });
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, ")"));
            }
        }

        private async Task _DeleteModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_DETAIL");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
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

                    if (m.Delete())
                    {
                        await cc.JsonAsync();
                    }
                    else
                    {
                        await cc.JsonAsync(-1, null, "指定模型数据不存在。");
                    }
                }
                finally
                {
                    mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                    {
                        return Math.Max(0, itemValue - 1);
                    });
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, ")"));
            }
        }

        private async Task _UpdateModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_UPDATE");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
                {
                    Model m = OrmUtils.Model(item.Model);

                    string id = cc.RouteParams["id"];
                    string[] ids = id.Split("|");
                    if (ids.Length != m.PrimaryKeyCount)
                    {
                        throw new Exception("无效的参数。");
                    }

                    dynamic obj = m.NewObject();

                    for (int i = 0; i < m.PrimaryKeyCount; i++)
                    {
                        Property p = m.GetPrimaryKey(i);
                        m.Equals(p.Name, ids[i]);
                    }

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

                    m.SetValues(obj).Update();

                    await cc.JsonAsync();
                }
                finally
                {
                    mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                    {
                        return Math.Max(0, itemValue - 1);
                    });
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, ")"));
            }
        }

        private void _MountModelRouters(RouterConfig.RouterItem item, RouteBuilder builder)
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

            //新建
            builder.MapPost(item.Path, async (context) =>
            {
                await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_CreateModelAsync), context, item);
            });

            //查列表
            builder.MapGet(item.Path, async (context) =>
            {
                await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_QueryModelListAsync), context, item);
            });

            //查详情
            builder.MapGet(individualPath, async (context) =>
            {
                await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_QueryModelDetailAsync), context, item);
            });

            //删除
            builder.MapDelete(individualPath, async (context) =>
            {
                await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_DeleteModelAsync), context, item);
            });

            //修改
            builder.MapPut(individualPath, async (context) =>
            {
                await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_UpdateModelAsync), context, item);
            });
        }

        #endregion

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
