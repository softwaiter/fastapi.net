using CodeM.Common.Ioc;
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

        long mAllHandlerCounter = 0;    //全部请求处理器计数，总阀门控制

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
                bool isControllerExists = true;

                try
                {
                    result = IocUtils.GetObject<object>(handlerClass);
                    if (result == null)
                    {
                        _DecHandlerCount(handlerFullName);
                        isControllerExists = false;
                    }
                }
                catch
                {
                    _DecHandlerCount(handlerFullName);
                }

                if (!isControllerExists)
                {
                    throw new Exception("路由处理器不存在。");
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

        public async Task<object> InvokeAsync(string handlerFullName, ControllerContext cc,
            int maxConcurrent, int maxIdle, int maxInvokePerInstance)
        {
            int pos = handlerFullName.LastIndexOf(".");
            if (pos <= 0)
            {
                cc.State = 500; //路由处理器配置错误
                return null;
            }
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
                            result = handlerInst.GetType().InvokeMember(handlerMethod,
                                BindingFlags.IgnoreCase | BindingFlags.Public | 
                                BindingFlags.Instance | BindingFlags.InvokeMethod,
                                null, handlerInst, new object[] { cc });

                            _IncHandlerExecNum(handlerInst);
                        }
                        catch (MissingMethodException)
                        {
                            cc.State = 501; //路由对应处理方法不存在
                        }
                        catch
                        {
                            cc.State = 500; //执行路由处理方法异常
                        }
                        finally
                        {
                            await _ReleaseHandler(handlerFullName, handlerInst, maxIdle, maxInvokePerInstance);
                        }
                    }
                    else
                    {
                        cc.State = 503;   //路由繁忙，暂时不可用
                    }
                }
                catch
                {
                    cc.State = 500; //创建路由处理器异常
                }
                finally
                {
                    Interlocked.Decrement(ref mAllHandlerCounter);
                }
            }
            else
            {
                Interlocked.Decrement(ref mAllHandlerCounter);
                cc.State = 503;   //系统繁忙，暂时不可用
            }

            return result;
        }
    }
}