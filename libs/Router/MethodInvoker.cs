using CodeM.Common.Ioc;
using CodeM.FastApi.Common;
using CodeM.FastApi.Context;
using System;
using System.Collections.Concurrent;
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
                    result = Wukong.GetObject<object>(handlerClass);
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

        private void _ReleaseHandler(string handlerFullName, object handler, 
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
        }

        public object Invoke(string handlerFullName, ControllerContext cc,
            int maxConcurrent, int maxIdle, int maxInvokePerInstance, bool ignoreMethodNotExists = false)
        {
            int pos = handlerFullName.LastIndexOf(".");
            string handlerMethod = handlerFullName.Substring(pos + 1);

            dynamic result = null;

            object handlerInst = _GetHandler(handlerFullName, maxConcurrent);
            if (handlerInst != null)
            {
                try
                {
                    Type handlerType = handlerInst.GetType();

                    if (ignoreMethodNotExists && !FastApiUtils.IsMethodExists(handlerType, handlerMethod))
                    {
                        return null;
                    }

                    result = handlerType.InvokeMember(handlerMethod,
                        BindingFlags.IgnoreCase | BindingFlags.Public | 
                        BindingFlags.Instance | BindingFlags.InvokeMethod,
                        null, handlerInst, new object[] { cc });

                    if (result != null)
                    {
                        Type _resultTyp = result.GetType();
                        if (_resultTyp.IsGenericType)
                        {
                            if (_resultTyp.GetGenericTypeDefinition() == typeof(Task<>))
                            {
                                if (result.Exception != null)
                                {
                                    if (FastApiUtils.IsDev())
                                    {
                                        throw result.Exception;
                                    }
                                }
                                else
                                {
                                    Type[] _typs = _resultTyp.GetGenericArguments();
                                    if (_typs.Length == 1 && 
                                        string.Compare(_typs[0].Name, "VoidTaskResult", true) != 0)
                                    {
                                        result = result.Result;
                                    }
                                    else
                                    {
                                        result = null;
                                    }
                                }
                            }
                        }
                    }

                    _IncHandlerExecNum(handlerInst);
                }
                finally
                {
                    _ReleaseHandler(handlerFullName, handlerInst, maxIdle, maxInvokePerInstance);
                }
            }
            else
            {
                throw new Exception(string.Concat("Router busy(", cc.Request.Method, " ", cc.Request.Path, "): ", maxConcurrent));
            }

            return result;
        }
    }
}