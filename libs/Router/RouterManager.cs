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
        private ApplicationConfig mAppConfig;

        private RouterConfig mRouterConfig;
        private MethodInvoker mMethodInvoker;

        public static RouterManager Current { get; } = new RouterManager();

        private RouterManager()
        {
            mRouterConfig = new RouterConfig();
            mMethodInvoker = new MethodInvoker();
        }

        public void Init(ApplicationConfig config, string routerFile)
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
                throw new Exception(string.Concat("System busy: ", mAppConfig.Router.MaxConcurrentTotal, "."));
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
            if (resouceInst == null)
            {
                throw new Exception(string.Concat("Instantiation exception(", item.Resource, ")"));
            }

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
                    return result != null ? result.ToString() : null;
                }
            }
            return null;
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

                    if (cc.PostJson != null && cc.PostJson.Has("_items") &&
                        cc.PostJson._items is List<dynamic> &&
                        item.ModelBatchAction.Contains("C"))
                    {
                        List<dynamic> newObjs = new List<dynamic>();
                        int count = m.PropertyCount;
                        for (int i = 0; i < Math.Min(100, cc.PostJson._items.Count); i++)
                        {
                            dynamic postItem = cc.PostJson._items[i];
                            dynamic obj = m.NewObject();
                            for (int j = 0; j < count; j++)
                            {
                                Property p = m.GetProperty(j);
                                if (!p.AutoIncrement)
                                {
                                    string v = null;
                                    if (postItem.Has(p.Name))
                                    {
                                        object result;
                                        if (postItem.TryGetValue(p.Name, out result))
                                        {
                                            v = result != null ? result.ToString() : null;
                                        }
                                    }

                                    if (v != null)
                                    {
                                        obj.SetValue(p.Name, v);
                                    }
                                }
                            }
                            newObjs.Add(obj);
                        }

                        int transCode = m.GetTransaction();
                        try
                        {
                            for (int i = 0; i < newObjs.Count; i++)
                            {
                                m.SetValues(newObjs[i]).Save(transCode, true);
                            }
                            OrmUtils.CommitTransaction(transCode);
                        }
                        catch (Exception exp)
                        {
                            OrmUtils.RollbackTransaction(transCode);
                            throw exp;
                        }
                    }
                    else
                    {
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
                    }

                    await cc.JsonAsync();
                }
                catch (Exception exp)
                {
                    if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await cc.JsonAsync(-1, null, "提交数据主键冲突，请修改后重试。");
                    }
                    else
                    {
                        throw exp;
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
                string prevOP = null;

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
                        if (!("AND".Equals(prevOP) &&
                            "OR".Equals(prevOP)))
                        {
                            SubFilter andFilter = new SubFilter();
                            current.And(andFilter);
                            current = andFilter;
                        }

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

                    prevOP = curOP.ToUpper();
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
                    int pagesize = 50;
                    int.TryParse(cc.QueryParams.Get("pagesize", "50"), out pagesize);
                    pagesize = Math.Min(pagesize, 200); //安全考虑，最大每页数据不能超过200条

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

                    string source = cc.QueryParams.Get("source", null);
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        m.GetValue(source.Split(","));
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

                    List<dynamic> list = m.And(filter).PageSize(pagesize).PageIndex(pageindex).Query();
                    await cc.JsonAsync(new
                    {
                        list,
                        total
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

        private async Task _BatchDeleteModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_DELETE_BATCH");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
                {
                    Model m = OrmUtils.Model(item.Model);

                    for (int i = 0; i < m.PrimaryKeyCount; i++)
                    {
                        Property p = m.GetPrimaryKey(i);
                        string values = cc.QueryParams.Get(p.Name, null);
                        if (values != null)
                        {
                            object[] items = values.Split(",");
                            m.In(p.Name, items);
                        }
                    }

                    m.Delete();

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

        private async Task _DeleteModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_DELETE");
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

        private async Task _BatchUpdateModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_UPDATE_BATCH");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                try
                {
                    Model m = OrmUtils.Model(item.Model);

                    for (int i = 0; i < m.PrimaryKeyCount; i++)
                    {
                        Property p = m.GetPrimaryKey(i);
                        string values = cc.QueryParams.Get(p.Name, null);
                        if (values != null)
                        {
                            object[] items = values.Split(",");
                            m.In(p.Name, items);
                        }
                    }

                    int count = m.PropertyCount;
                    dynamic obj = m.NewObject();
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
                catch (Exception exp)
                {
                    if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await cc.JsonAsync(-1, null, "提交数据主键冲突，请修改后重试。");
                    }
                    else
                    {
                        throw exp;
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
                catch (Exception exp)
                {
                    if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await cc.JsonAsync(-1, null, "提交数据主键冲突，请修改后重试。");
                    }
                    else
                    {
                        throw exp;
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
            if (item.ModelAction.Contains("C") ||
                item.ModelBatchAction.Contains("C"))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_CreateModelAsync), context, item);
                });
            }

            //查列表
            if (item.ModelAction.Contains("L"))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_QueryModelListAsync), context, item);
                });
            }

            //查详情
            if (item.ModelAction.Contains("D"))
            {
                builder.MapGet(individualPath, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_QueryModelDetailAsync), context, item);
                });
            }

            //删除
            if (item.ModelAction.Contains("R"))
            {
                builder.MapDelete(individualPath, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_DeleteModelAsync), context, item);
                });
            }

            //批量删除
            if (item.ModelBatchAction.Contains("R"))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_BatchDeleteModelAsync), context, item);
                });
            }

            //修改
            if (item.ModelAction.Contains("U"))
            {
                builder.MapPut(individualPath, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_UpdateModelAsync), context, item);
                });
            }

            //批量修改
            if (item.ModelBatchAction.Contains("U"))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await _ThroughRequestPipelineAsync(new ControllerInvokeDelegate(_BatchUpdateModelAsync), context, item);
                });
            }
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

            //通过排序，将路由路径更长的放到前面
            mRouterConfig.Items.Sort((left, right) => 
            {
                if (left.Path.Length > right.Path.Length)
                {
                    return -1;
                }
                else if (left.Path.Length < right.Path.Length)
                {
                    return 1;
                }
                return 0;
            });

            mRouterConfig.Items.ForEach(item =>
            {
                this._MountRouters(item, builder);
            });

            IRouter router = builder.Build();
            app.UseRouter(router);
        }
    }
}
