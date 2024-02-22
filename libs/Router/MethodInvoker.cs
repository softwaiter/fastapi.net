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
        readonly ConcurrentDictionary<string, ConcurrentStack<object>> mHandlers = new ConcurrentDictionary<string, ConcurrentStack<object>>();
        readonly ReaderWriterLockSlim handlerLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        readonly ConcurrentDictionary<string, int> mHandlerCounters = new ConcurrentDictionary<string, int>();
        readonly ConcurrentDictionary<string, int> mHandlerExecNumbers = new ConcurrentDictionary<string, int>();

        private ConcurrentStack<object> GetHandlerStack(string handlerFullName)
        {
            string key = handlerFullName.ToLower();
            if (!mHandlers.TryGetValue(key, out ConcurrentStack<object> result))
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

        private bool IncHandlerCount(string handlerFullName, int maxConcurrent)
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

        private bool DecHandlerCount(string handlerFullName)
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

        private object GetHandler(string handlerFullName, int maxConcurrent)
        {
            object result = null;

            int pos = handlerFullName.LastIndexOf(".");
            string handlerClass = handlerFullName.Substring(0, pos);

            ConcurrentStack<object> stack = GetHandlerStack(handlerFullName);
            if (stack != null && !stack.IsEmpty)
            {
                stack.TryPop(out result);
            }

            if (result == null && IncHandlerCount(handlerFullName, maxConcurrent))
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
                    DecHandlerCount(handlerFullName);
                    throw new Exception(string.Concat("Instantiation exception(", handlerFullName, ")"));
                }
            }

            return result;
        }

        private void IncHandlerExecNum(object handler)
        {
            mHandlerExecNumbers.AddOrUpdate("handler_" + handler.GetHashCode(), 1, (key, value) =>
            {
                return value + 1;
            });
        }

        private bool IsNotExpired(object handler, int maxInvokePerInstance)
        {
            bool result = true;

            mHandlerExecNumbers.TryGetValue("handler_" + handler.GetHashCode(), out int execNum);
            if (execNum >= maxInvokePerInstance)
            {
                result = false;
            }

            return result;
        }

        private void ReleaseHandler(string handlerFullName, object handler, 
            int maxIdle, int maxInvokePerInstance)
        {
            ConcurrentStack<object> stack = GetHandlerStack(handlerFullName);
            if (stack != null && stack.Count < maxIdle && IsNotExpired(handler, maxInvokePerInstance))
            {
                stack.Push(handler);
            }
            else
            {
                mHandlerExecNumbers.TryRemove("handler_" + handler.GetHashCode(), out int execNum);
                DecHandlerCount(handlerFullName);
            }
        }

        public object Invoke(string handlerFullName, ControllerContext cc,
            int maxConcurrent, int maxIdle, int maxInvokePerInstance, bool ignoreMethodNotExists = false)
        {
            int pos = handlerFullName.LastIndexOf(".");
            string handlerMethod = handlerFullName.Substring(pos + 1);

            dynamic result = null;

            object handlerInst = GetHandler(handlerFullName, maxConcurrent);
            if (handlerInst != null)
            {
                try
                {
                    Type handlerType = handlerInst.GetType();

                    if (!FastApiUtils.IsMethodExists(handlerType, handlerMethod))
                    {
                        if (ignoreMethodNotExists)
                        {
                            return null;
                        }
                        else
                        {
                            throw new Exception(String.Concat("Router processor not found: ", handlerFullName));
                        }
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

                    IncHandlerExecNum(handlerInst);
                }
                finally
                {
                    ReleaseHandler(handlerFullName, handlerInst, maxIdle, maxInvokePerInstance);
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