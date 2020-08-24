using CodeM.Common.Ioc;
using CodeM.FastApi.Common;
using CodeM.FastApi.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeM.FastApi.Router
{
    internal class MethodInvoker
    {
        ConcurrentDictionary<string, ConcurrentStack<object>> mHandlers = new ConcurrentDictionary<string, ConcurrentStack<object>>();
        ReaderWriterLockSlim handlerLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        ConcurrentDictionary<string, int> mHandlerCounters = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, int> mHandlerExecNumbers = new ConcurrentDictionary<string, int>();

        long mAllHandlerCounter = 0;    //全部请求处理器计数，总阀门控制

        Dictionary<string, bool> mTypeMethods = new Dictionary<string, bool>();

        private ConcurrentStack<object> _GetHandlerStack(string handlerFullName)
        {
            ConcurrentStack<object> result = null;

            string key = handlerFullName.ToLower();
            if (!mHandlers.TryGetValue(key, out result))
            {
                handlerLocker.EnterWriteLock();
                try
                {
                    if (!mHandlers.TryGetValue(key, out result))
                    {
                        result = new ConcurrentStack<object>();
                        mHandlers.TryAdd(key, result);
                    }
                }
                finally
                {
                    handlerLocker.ExitWriteLock();
                }
            }

            return result;
        }

        private bool _IncHandlerCount(string handlerFullName, int maxConcurrent)
        {
            bool result = true;

            if (maxConcurrent > 0)
            {
                string key = handlerFullName.ToLower();
                mHandlerCounters.AddOrUpdate(key, 1, (key, value) =>
                {
                    if (value < maxConcurrent)
                    {
                        return value + 1;
                    }
                    else
                    {
                        result = false;
                        return value;
                    }
                });
            }
            else
            {
                result = false;
            }

            return result;
        }

        private bool _DecHandlerCount(string handlerFullName)
        {
            bool result = true;

            string key = handlerFullName.ToLower();
            mHandlerCounters.AddOrUpdate(key, 0, (key, value) =>
            {
                if (value > 0)
                {
                    return value - 1;
                }
                else
                {
                    return 0;
                }
            });

            return result;
        }

        private object _GetHandler(string handlerFullName, int maxConcurrent)
        {
            object result = null;

            int pos = handlerFullName.LastIndexOf(".");
            string handlerClass = handlerFullName.Substring(0, pos);

            ConcurrentStack<object> stack = _GetHandlerStack(handlerFullName);
            if (stack != null && !stack.IsEmpty)
            {
                stack.TryPop(out result);
            }

            if (result == null && _IncHandlerCount(handlerFullName, maxConcurrent))
            {
                try
                {
                    result = IocUtils.GetObject<object>(handlerClass);
                    if (result == null)
                    {
                        throw new Exception();
                    }
                }
                catch
                {
                    _DecHandlerCount(handlerFullName);
                    throw new Exception(string.Concat("Instantiation exception(", handlerFullName, ")"));
                }
            }

            return result;
        }

        private void _IncHandlerExecNum(object handler)
        {
            mHandlerExecNumbers.AddOrUpdate("handler_" + handler.GetHashCode(), 1, (key, value) =>
            {
                return value + 1;
            });
        }

        private bool _IsNotExpired(object handler, int maxInvokePerInstance)
        {
            bool result = true;

            int execNum = 0;
            mHandlerExecNumbers.TryGetValue("handler_" + handler.GetHashCode(), out execNum);
            if (execNum >= maxInvokePerInstance)
            {
                result = false;
            }

            return result;
        }

        private async Task _ReleaseHandler(string handlerFullName, object handler, 
            int maxIdle, int maxInvokePerInstance)
        {
            ConcurrentStack<object> stack = _GetHandlerStack(handlerFullName);
            if (stack != null && stack.Count < maxIdle && _IsNotExpired(handler, maxInvokePerInstance))
            {
                stack.Push(handler);
            }
            else
            {
                int execNum;
                mHandlerExecNumbers.TryRemove("handler_" + handler.GetHashCode(), out execNum);

                _DecHandlerCount(handlerFullName);
            }

            await Task.CompletedTask;
        }

        private async Task<bool> _methodIsExists(Type _typ, string methodName)
        {
            string key = string.Concat(_typ.FullName, "`", methodName);
            if (!mTypeMethods.ContainsKey(key))
            {
                MethodInfo mi = _typ.GetMethod(methodName, 
                    BindingFlags.Instance | BindingFlags.Public | 
                    BindingFlags.IgnoreCase);
                mTypeMethods[key] = mi != null;
            }
            await Task.CompletedTask;
            return mTypeMethods[key];
        }

        public async Task<object> InvokeAsync(string handlerFullName, ControllerContext cc,
            int maxConcurrent, int maxIdle, int maxInvokePerInstance, bool ignoreMethodNotExists = false)
        {
            int pos = handlerFullName.LastIndexOf(".");
            string handlerMethod = handlerFullName.Substring(pos + 1);

            object result = null;

            if (Interlocked.Increment(ref mAllHandlerCounter) <= cc.Config.Router.MaxConcurrentTotal)
            {
                try
                {
                    object handlerInst = _GetHandler(handlerFullName, maxConcurrent);
                    if (handlerInst != null)
                    {
                        try
                        {
                            Type handlerType = handlerInst.GetType();

                            if (ignoreMethodNotExists && !await _methodIsExists(handlerType, handlerMethod))
                            {
                                return null;
                            }

                            result = handlerType.InvokeMember(handlerMethod,
                                BindingFlags.IgnoreCase | BindingFlags.Public | 
                                BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null, handlerInst, new object[] { cc });

                            if (await Utils.IsDevelopment())
                            {
                                Type _resultTyp = result.GetType();
                                if (_resultTyp.IsGenericType)
                                {
                                    if (_resultTyp.GetGenericTypeDefinition() == typeof(Task<>))
                                    {
                                        Task _taskResult = (Task)result;
                                        if (_taskResult.Exception != null)
                                        {
                                            throw _taskResult.Exception;
                                        }
                                    }
                                }
                            }

                            _IncHandlerExecNum(handlerInst);
                        }
                        finally
                        {
                            await _ReleaseHandler(handlerFullName, handlerInst, maxIdle, maxInvokePerInstance);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Concat("Router busy(", cc.Request.Path, ")"));
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

            return result;
        }
    }
}