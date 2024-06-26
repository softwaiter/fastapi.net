﻿using CodeM.Common.Ioc;
using CodeM.Common.Orm;
using CodeM.Common.Tools.DynamicObject;
using CodeM.FastApi.Common;
using CodeM.FastApi.Config;
using CodeM.FastApi.Context;
using CodeM.FastApi.Log;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeM.FastApi.Router
{
    public class RouterManager
    {
        private ApplicationConfig mAppConfig;

        private readonly RouterConfig mRouterConfig;
        private readonly MethodInvoker mMethodInvoker;

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
        private async Task ThroughRequestPipelineAsync(ControllerInvokeDelegate ControllerDelegate, 
            HttpContext context, RouterConfig.RouterItem item, params object[] args)
        {
            if (Interlocked.Increment(ref mAllHandlerCounter) <= mAppConfig.Router.MaxConcurrentTotal)
            {
                ControllerContext cc = ControllerContext.FromHttpContext(context, mAppConfig);

                try
                {
                    bool breaked = false;
                    Stack<string> _responseMiddlewares = new Stack<string>();

                    List<string> middlewares = new List<string>();
                    middlewares.AddRange(mAppConfig.Middlewares);
                    middlewares.AddRange(item.Middlewares);
                    foreach (string middleware in middlewares)
                    {
                        string middlewareMethod = string.Concat(middleware, ".Request");
                        object r = mMethodInvoker.Invoke(middlewareMethod, cc,
                            int.MaxValue, mAppConfig.Router.MaxIdlePerRouter,
                            mAppConfig.Router.MaxInvokePerInstance, true);

                        _responseMiddlewares.Push(middleware);

                        if (r != null &&
                            r.GetType() == typeof(bool) &&
                            (bool)r == false)
                        {
                            breaked = true;
                            break;
                        }
                    }

                    if (!breaked)
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
                    if (FastApiUtils.IsProd())
                    {
                        Logger.Instance().Error(exp);
                        await cc.JsonAsync(-1, null, "服务异常，请按F5刷新重试。");
                    }
                    else
                    {
                        cc.State = 500;
                        await cc.Response.WriteAsync(exp.ToString(), Encoding.UTF8);
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

        private string ProcessApiVersion(ControllerContext cc,
            string handlerFullName)
        {
            if (cc.Config.VersionControl.Enable)
            {
                string version = cc.Headers.Get(cc.Config.VersionControl.Param, null);
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = cc.QueryParams.Get(cc.Config.VersionControl.Param, null);
                }
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = cc.Config.VersionControl.Default;
                }

                version = version.Trim();
                if (!version.Equals(cc.Config.VersionControl.Default, StringComparison.OrdinalIgnoreCase))
                {
                    if (cc.Config.VersionControl.AllowedVersions != null &&
                        cc.Config.VersionControl.AllowedVersions.Length > 0)
                    {
                        if (!Array.Exists<string>(cc.Config.VersionControl.AllowedVersions, 
                            item => version.Equals(item, StringComparison.OrdinalIgnoreCase)))
                        {
                            version = "";
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        handlerFullName += string.Concat("_", version);
                    }
                }
            }
            return handlerFullName;
        }

        private async Task DefaultRouterHandlerAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string handlerFullName = ProcessApiVersion(cc, "" + args[0]);
            mMethodInvoker.Invoke(handlerFullName, cc,
                item.MaxConcurrent, item.MaxIdle, item.MaxInvokePerInstance);
            await Task.CompletedTask;
        }

        private void MountSingleHandler(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string method = !string.IsNullOrWhiteSpace(item.Method) ? item.Method.ToUpper() : "GET";

            if ("GET".Equals(method))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync), 
                        context, item, item.Handler);
                });
            }
            else if ("POST".Equals(method))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
            else if ("PUT".Equals(method))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
            else if ("DELETE".Equals(method))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, item.Handler);
                });
            }
        }

        #region Resource Router
        private void MountResourceRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            object resouceInst = Wukong.GetObject(item.Resource);
            if (resouceInst == null)
            {
                throw new Exception(string.Concat("Instantiation exception(", item.Resource, ")"));
            }

            string individualPath = item.Path;
            if (individualPath.EndsWith("/"))
            {
                individualPath += "{id:int}";
            }
            else
            {
                individualPath += "/{id:int}";
            }

            //增
            if (FastApiUtils.IsMethodExists(resouceInst, "Create"))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Create");
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //删
            if (FastApiUtils.IsMethodExists(resouceInst, "Delete"))
            {
                builder.MapDelete(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Delete");
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //改
            if (FastApiUtils.IsMethodExists(resouceInst, "Update"))
            {
                builder.MapPut(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Update");
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //查（列表）
            if (FastApiUtils.IsMethodExists(resouceInst, "List"))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".List");
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }

            //查（个体）
            if (FastApiUtils.IsMethodExists(resouceInst, "Detail"))
            {
                builder.MapGet(individualPath, async (context) =>
                {
                    string handlerFullName = string.Concat(item.Resource, ".Detail");
                    await ThroughRequestPipelineAsync(
                        new ControllerInvokeDelegate(DefaultRouterHandlerAsync),
                        context, item, handlerFullName);
                });
            }
        }

        #endregion

        #region Model Router

        private object GetParamValue(ControllerContext cc, string name)
        {
            if (cc.PostJson != null && cc.PostJson.Has(name))
            {
                if (cc.PostJson.TryGetValue(name, out object result))
                {
                    return result;
                }
            }
            return null;
        }

        private async Task HandleDuplicateException(ControllerContext cc, string modelName, dynamic modelObj, Exception exp)
        {
            Model m = Derd.Model(modelName);
            for (int i = m.PropertyCount - 1; i >= 0; i--)
            {
                Property p = m.GetProperty(i);
                if (exp.Message.Contains(p.Field))
                {
                    string value = string.Empty;
                    if (modelObj != null && modelObj.Has(p.Name))
                    {
                        value = modelObj.GetValue(p.Name);
                    }
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        string item = !string.IsNullOrWhiteSpace(p.Label) ? p.Label : p.Name;
                        await cc.JsonAsync(-1, null, item + " “" + value + "” 已存在！");
                    }
                    else
                    {
                        string item = !string.IsNullOrWhiteSpace(p.Label) ? p.Label : p.Name;
                        await cc.JsonAsync(-1, null, item + "内容已存在！");
                    }
                    return;
                }
            }
            await cc.JsonAsync(-1, null, "提交数据存在重复项，请检查后重试。");
        }

        private async Task HandleNotNullException(ControllerContext cc, string modelName, dynamic modelObj, Exception exp)
        {
            Model m = Derd.Model(modelName);
            for (int i = m.PropertyCount - 1; i >= 0; i--)
            {
                Property p = m.GetProperty(i);
                if (exp.Message.Contains(p.Field))
                {
                    string item = !string.IsNullOrWhiteSpace(p.Label) ? p.Label : p.Name;
                    await cc.JsonAsync(-1, null, item + "不能为空！");
                    return;
                }
            }
            await cc.JsonAsync(-1, null, "提交内容缺少必填项，请检查后重试。");
        }

        private async Task CreateModelAsync(ControllerContext cc, 
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_CREATE");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                dynamic catchObj = null;
                try
                {
                    bool bRet = true;
                    Model m = Derd.Model(item.Model);
                    if (cc.PostJson != null && cc.PostJson.Has("_items") &&
                        cc.PostJson._items is List<dynamic> &&
                        item.ModelBatchAction.Contains("C"))
                    {
                        List<dynamic> newObjs = new List<dynamic>();
                        int count = m.PropertyCount;
                        for (int i = 0; i < Math.Min(100, cc.PostJson._items.Count); i++)
                        {
                            dynamic postItem = cc.PostJson._items[i];
                            dynamic obj = new DynamicObjectExt();
                            for (int j = 0; j < count; j++)
                            {
                                Property p = m.GetProperty(j);
                                if (!p.AutoIncrement)
                                {
                                    string v = null;
                                    if (postItem.Has(p.Name))
                                    {
                                        if (postItem.TryGetValue(p.Name, out object result))
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
                                catchObj = newObjs[i];
                                bRet = m.SetValues(newObjs[i], true).Save(transCode);
                                if (!bRet)
                                {
                                    break;
                                }
                            }
                            Derd.CommitTransaction(transCode);
                        }
                        catch
                        {
                            Derd.RollbackTransaction(transCode);
                            throw;
                        }
                    }
                    else
                    {
                        dynamic obj = new DynamicObjectExt();
                        int count = m.PropertyCount;
                        for (int i = 0; i < count; i++)
                        {
                            Property p = m.GetProperty(i);
                            if (!p.AutoIncrement)
                            {
                                object v = GetParamValue(cc, p.Name);
                                if (v != null)
                                {
                                    obj.SetValue(p.Name, v);
                                }
                            }
                        }

                        catchObj = obj;
                        bRet = m.SetValues(obj, true).Save();
                    }

                    if (bRet)
                    {
                        await cc.JsonAsync();
                    }
                    else
                    {
                        await cc.JsonAsync(-1, null, "保存数据失败，请检查后重试。");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException ||
                        exp is PropertyValidationException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDuplicateException(cc, item.Model, catchObj, exp);
                    }
                    else if (exp.Message.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("无法将 NULL 插入", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("违反了非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("不能将值 NULL 插入列", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("doesn't have a default value", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleNotNullException(cc, item.Model, catchObj, exp);
                    }
                    else
                    {
                        throw;
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

        private static Type DbType2Type(DbType type)
        {
            if (type == DbType.String ||
                type == DbType.AnsiString ||
                type == DbType.AnsiStringFixedLength ||
                type == DbType.StringFixedLength)
            {
                return typeof(string);
            }
            else if (type == DbType.Boolean)
            {
                return typeof(bool);
            }
            else if (type == DbType.Byte)
            {
                return typeof(byte);
            }
            else if (type == DbType.SByte)
            {
                return typeof(sbyte);
            }
            else if (type == DbType.Decimal)
            {
                return typeof(decimal);
            }
            else if (type == DbType.Double)
            {
                return typeof(double);
            }
            else if (type == DbType.Int16)
            {
                return typeof(Int16);
            }
            else if (type == DbType.Int32)
            {
                return typeof(Int32);
            }
            else if (type == DbType.Int64)
            {
                return typeof(Int64);
            }
            else if (type == DbType.Single)
            {
                return typeof(Single);
            }
            else if (type == DbType.UInt16)
            {
                return typeof(UInt16);
            }
            else if (type == DbType.UInt32)
            {
                return typeof(UInt32);
            }
            else if (type == DbType.UInt64)
            {
                return typeof(UInt64);
            }
            else if (type == DbType.Date ||
                type == DbType.DateTime ||
                type == DbType.DateTime2 ||
                type == DbType.DateTimeOffset)
            {
                return typeof(DateTime);
            }
            return typeof(Object);
        }

        private readonly Regex mReOP = new Regex("\\(|\\)|\\s+AND\\s+|\\s+OR\\s+|>=|<=|<>|~!=|^!=|!=\\$|!=|\\*=|~=|\\^=|=\\$|>|<|=", RegexOptions.IgnoreCase);

        private void BuildWhereFilter(Model m, IFilter filter, string op, string name, string value)
        {
            if (!m.HasProperty(name) && !m.HasProperty(value))
            {
                filter.And(String.Concat(name, op, value));
                return;
            }

            string exprKey = name;
            object exprValue = value;
            if (">=".Equals(op) ||
                "<=".Equals(op) ||
                "<>".Equals(op) ||
                "<".Equals(op) ||
                ">".Equals(op) ||
                "=".Equals(op) ||
                "!=".Equals(op))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (value.StartsWith("@"))  // 表示表达式值是对象属性
                    {
                        exprValue = Funcs.PROPERTY(value.Substring(1));
                    }
                    else
                    {
                        if (value.StartsWith("\\@"))    // 表达式值起始为@时采用\\转义写法，进行恢复处理
                        {
                            value = value.Substring(1);
                        }

                        Property p = m.GetProperty(name);
                        Type type = DbType2Type(p.FieldType);
                        exprValue = Convert.ChangeType(value, type);
                    }
                }
            }

            switch (op)
            {
                case ">=":
                    filter.Gte(exprKey, exprValue);
                    break;
                case "<=":
                    filter.Lte(exprKey, exprValue);
                    break;
                case "<>":
                case "!=":
                    filter.NotEquals(exprKey, exprValue);
                    break;
                case "*=":
                    string[] items = value.Split(',');
                    if (items.Length != 2)
                    {
                        throw new Exception("Between参数无效。");
                    }

                    Property p = m.GetProperty(name);
                    Type type = DbType2Type(p.FieldType);
                    object value1 = Convert.ChangeType(items[0], type);
                    object value2 = Convert.ChangeType(items[1], type);

                    filter.Between(exprKey, value1, value2);
                    break;
                case ">":
                    filter.Gt(exprKey, exprValue);
                    break;
                case "<":
                    filter.Lt(exprKey, exprValue);
                    break;
                case "=":
                    filter.Equals(exprKey, exprValue);
                    break;
                case "~=":
                    filter.Like(exprKey, exprValue);
                    break;
                case "~!=":
                    filter.NotLike(exprKey, exprValue);
                    break;
                case "^=":
                    List<object> inParams = new List<object>();
                    object[] inValues = value.Split(",");

                    Property inProp = m.GetProperty(name);
                    Type inType = DbType2Type(inProp.FieldType);
                    foreach (object item in inValues)
                    {
                        inParams.Add(Convert.ChangeType(item, inType));
                    }

                    filter.In(exprKey, inParams.ToArray());
                    break;
                case "^!=":
                    List<object> notinParams = new List<object>();
                    object[] notinValues = value.Split(",");

                    Property notinProp = m.GetProperty(name);
                    Type notinType = DbType2Type(notinProp.FieldType);
                    foreach (object item in notinValues)
                    {
                        notinParams.Add(Convert.ChangeType(item, notinType));
                    }

                    filter.NotIn(exprKey, notinParams.ToArray());
                    break;
                case "=$":
                    filter.IsNull(exprKey);
                    break;
                case "!=$":
                    filter.IsNotNull(exprKey);
                    break;
            }
        }

        private IFilter ParseQueryWhereCondition(Model m, string where)
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
                Stack<IFilter> bracketEntryPoints = new Stack<IFilter>();

                MatchCollection mc = mReOP.Matches(where);
                for (int i = 0; i < mc.Count; i++)
                {
                    Match match = mc[i];

                    string curOP = match.Value.Trim();
                    if ("AND".Equals(curOP, StringComparison.OrdinalIgnoreCase))
                    {
                        if (exprName != null)
                        {
                            exprValue = where.Substring(offset, match.Index - offset).Trim();

                            BuildWhereFilter(m, current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        offset = match.Index + match.Length;

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
                            exprValue = where.Substring(offset, match.Index - offset).Trim();

                            BuildWhereFilter(m, current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        offset = match.Index + match.Length;

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
                        bracketEntryPoints.Push(current);

                        SubFilter andFilter = new SubFilter();
                        current.And(andFilter);
                        current = andFilter;

                        bracket++;
                        offset = match.Index + match.Length;
                    }
                    else if (")".Equals(curOP))
                    {
                        if (exprName != null)
                        {
                            exprValue = where.Substring(offset, match.Index - offset).Trim();

                            BuildWhereFilter(m, current, aop, exprName, exprValue);
                            exprName = null;
                        }

                        bracket--;
                        offset = match.Index + match.Length;

                        current = bracketEntryPoints.Pop();
                    }
                    else
                    {
                        exprName = where.Substring(offset, match.Index - offset).Trim();
                        aop = curOP;
                        offset = match.Index + match.Length;

                        if (i == mc.Count - 1)
                        {
                            exprValue = where.Substring(offset).Trim();

                            BuildWhereFilter(m, current, aop, exprName, exprValue);
                            exprName = null;
                        }
                    }
                }
            }

            return result;
        }

        private readonly ConcurrentDictionary<string, long> mModelRouterConcurrents = new ConcurrentDictionary<string, long>();
        private async Task QueryModelListAsync(ControllerContext cc, 
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
                    Model m = Derd.Model(item.Model);

                    int pagesize = 50;
                    int.TryParse(cc.QueryParams.Get("pagesize", "50"), out pagesize);
                    pagesize = Math.Min(pagesize, 200); //安全考虑，最大每页数据不能超过200条

                    int pageindex = 1;
                    int.TryParse(cc.QueryParams.Get("pageindex", "1"), out pageindex);

                    string where = cc.QueryParams.Get("where", null);
                    IFilter filter = ParseQueryWhereCondition(m, where);

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
                        m.GetValues(source.Split(","));
                    }

                    string sort = cc.QueryParams.AllValues("sort");
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
                catch (Exception exp)
                {
                    if (exp is FastApiException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else
                    {
                        throw;
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

        private async Task QueryModelDetailAsync(ControllerContext cc, 
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
                    Model m = Derd.Model(item.Model);

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

                    string where = cc.QueryParams.Get("where", null);
                    if (!string.IsNullOrWhiteSpace(where))
                    {
                        IFilter filter = ParseQueryWhereCondition(m, where);
                        m.And(filter);
                    }

                    string source = cc.QueryParams.Get("source", null);
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        m.GetValues(source.Split(","));
                    }

                    object detailObj = null;

                    List<dynamic> result = m.Top(1).Query();
                    if (result.Count > 0)
                    {
                        detailObj = result[0];
                    }

                    await cc.JsonAsync(detailObj);
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else
                    {
                        throw;
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

        private async Task BatchDeleteModelAsync(ControllerContext cc,
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
                    Model m = Derd.Model(item.Model);

                    for (int i = 0; i < m.PrimaryKeyCount; i++)
                    {
                        Property p = m.GetPrimaryKey(i);
                        string values = cc.QueryParams.AllValues(p.Name);
                        if (values != null)
                        {
                            object[] items = values.Split(",");
                            m.In(p.Name, items);
                        }
                    }

                    if (m.Delete())
                    {
                        await cc.JsonAsync();
                    }
                    else
                    {
                        await cc.JsonAsync(-1, null, "删除数据失败，请检查后重试。");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else
                    {
                        throw;
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

        private async Task DeleteModelAsync(ControllerContext cc,
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
                    Model m = Derd.Model(item.Model);

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
                        await cc.JsonAsync(-1, null, "数据删除失败，请检查后重试。");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else
                    {
                        throw;
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

        private async Task BatchUpdateModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_UPDATE_BATCH");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                dynamic catchObj = null;
                try
                {
                    Model m = Derd.Model(item.Model);

                    for (int i = 0; i < m.PrimaryKeyCount; i++)
                    {
                        Property p = m.GetPrimaryKey(i);
                        string values = cc.QueryParams.AllValues(p.Name);
                        if (values != null)
                        {
                            object[] items = values.Split(",");
                            m.In(p.Name, items);
                        }
                    }

                    int count = m.PropertyCount;
                    dynamic obj = new DynamicObjectExt();
                    for (int i = 0; i < count; i++)
                    {
                        Property p = m.GetProperty(i);
                        if (!p.AutoIncrement)
                        {
                            object v = GetParamValue(cc, p.Name);
                            if (v != null)
                            {
                                obj.SetValue(p.Name, v);
                            }
                        }
                    }

                    catchObj = obj;

                    if (m.SetValues(obj).Update())
                    {
                        await cc.JsonAsync();
                    }
                    else
                    {
                        await cc.JsonAsync(-1, null, "更新数据失败，请检查后重试。");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException || 
                        exp is PropertyValidationException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDuplicateException(cc, item.Model, catchObj, exp);
                    }
                    else if (exp.Message.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("无法将 NULL 插入", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("违反了非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("不能将值 NULL 插入列", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("doesn't have a default value", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleNotNullException(cc, item.Model, catchObj, exp);
                    }
                    else
                    {
                        throw;
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

        private async Task UpdateModelAsync(ControllerContext cc,
            RouterConfig.RouterItem item, params object[] args)
        {
            string key = string.Concat(item.Model, "_UPDATE");
            if (mModelRouterConcurrents.GetOrAdd(key, 0) <= item.MaxConcurrent)
            {
                mModelRouterConcurrents.AddOrUpdate(key, 1, (itemKey, itemValue) =>
                {
                    return itemValue + 1;
                });

                dynamic catchObj = null;
                try
                {
                    Model m = Derd.Model(item.Model);

                    string id = cc.RouteParams["id"];
                    string[] ids = id.Split("|");
                    if (ids.Length != m.PrimaryKeyCount)
                    {
                        throw new Exception("无效的参数。");
                    }

                    dynamic obj = new DynamicObjectExt();

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
                            object v = GetParamValue(cc, p.Name);
                            if (v != null)
                            {
                                obj.SetValue(p.Name, v);
                            }
                        }
                    }

                    catchObj = obj;

                    if (m.SetValues(obj, true).Update())
                    {
                        await cc.JsonAsync();
                    }
                    else
                    {
                        await cc.JsonAsync(-1, null, "更新数据失败，请检查后重试。");
                    }
                }
                catch (Exception exp)
                {
                    if (exp is FastApiException || 
                        exp is PropertyValidationException)
                    {
                        await cc.JsonAsync(-1, null, exp.Message);
                    }
                    else if (exp.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("DUPLICATE", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDuplicateException(cc, item.Model, catchObj, exp);
                    }
                    else if (exp.Message.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("无法将 NULL 插入", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("违反了非空约束", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("不能将值 NULL 插入列", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("doesn't have a default value", StringComparison.OrdinalIgnoreCase) ||
                        exp.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleNotNullException(cc, item.Model, catchObj, exp);
                    }
                    else
                    {
                        throw;
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

        private void MountModelRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            string individualPath = item.Path;
            if (individualPath.EndsWith("/"))
            {
                individualPath += "{id:int}";
            }
            else
            {
                individualPath += "/{id:int}";
            }

            //新建
            if (item.ModelAction.Contains("C") ||
                item.ModelBatchAction.Contains("C"))
            {
                builder.MapPost(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(CreateModelAsync), context, item);
                });
            }

            //查列表
            if (item.ModelAction.Contains("L"))
            {
                builder.MapGet(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(QueryModelListAsync), context, item);
                });
            }

            //查详情
            if (item.ModelAction.Contains("D"))
            {
                builder.MapGet(individualPath, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(QueryModelDetailAsync), context, item);
                });
            }

            //删除
            if (item.ModelAction.Contains("R"))
            {
                builder.MapDelete(individualPath, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(DeleteModelAsync), context, item);
                });
            }

            //批量删除
            if (item.ModelBatchAction.Contains("R"))
            {
                builder.MapDelete(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(BatchDeleteModelAsync), context, item);
                });
            }

            //修改
            if (item.ModelAction.Contains("U"))
            {
                builder.MapPut(individualPath, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(UpdateModelAsync), context, item);
                });
            }

            //批量修改
            if (item.ModelBatchAction.Contains("U"))
            {
                builder.MapPut(item.Path, async (context) =>
                {
                    await ThroughRequestPipelineAsync(new ControllerInvokeDelegate(BatchUpdateModelAsync), context, item);
                });
            }
        }

        #endregion

        private void MountRouters(RouterConfig.RouterItem item, RouteBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(item.Model))
            {
                MountModelRouters(item, builder);
            }
            else if (!string.IsNullOrWhiteSpace(item.Resource))
            {
                MountResourceRouters(item, builder);
            }
            else
            {
                MountSingleHandler(item, builder);
            }
        }

        readonly Regex reParamRoute = new Regex("\\{[^/]*?\\}");
        public void MountRouters(IApplicationBuilder app)
        {
            RouteBuilder builder = new RouteBuilder(app);

            if (mRouterConfig.Items != null)
            {
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
                    else
                    {
                        if (reParamRoute.IsMatch(left.Path))
                        {
                            return 1;
                        }
                        else if (reParamRoute.IsMatch(right.Path))
                        {
                            return -1;
                        }
                    }
                    return 0;
                });

                mRouterConfig.Items.ForEach(item =>
                {
                    this.MountRouters(item, builder);
                });
            }

            IRouter router = builder.Build();
            app.UseRouter(router);
        }
    }
}
