using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CodeM.FastApi.Log
{
    public class Logger
    {
        private static Logger sLogger = new Logger();

        private static ILoggerFactory sFactory;

        private static string[] sFilterMethos = new string[] { "MoveNext", "Start", "InvokeMethod" };
        public static string GetCallerName(int skipNum = 0)
        {
            string result = string.Empty;

            int skip = 1 + Math.Max(0, skipNum);
            StackTrace st = new StackTrace();
            StackFrame[] sfs = st.GetFrames();
            for (int i = 0; i < sfs.Length; i++)
            {
                StackFrame sf = sfs[i];
                if (StackFrame.OFFSET_UNKNOWN != sf.GetILOffset())
                {
                    MethodBase mb = sf.GetMethod();
                    if (!sFilterMethos.Contains<string>(mb.Name))
                    {
                        if (skip == 0)
                        {
                            result = mb.DeclaringType.FullName + "." + mb.Name;
                            break;
                        }
                        skip--;
                    }
                }
            }

            return result;
        }

        public static void Init(ILoggerFactory factory)
        {
            sFactory = factory;
        }

        public static bool Inited 
        { 
            get 
            {
                return sFactory != null;
            } 
        }

        public static Logger GetInstance()
        {
            return sLogger;
        }

        private static void _CheckInit()
        {
            if (sFactory == null)
            {
                throw new Exception("Please use Init method to initialize.");
            }
        }

        private static ILogger _GetLogger(string categoryName)
        {
            return sFactory.CreateLogger(categoryName);
        }

        private Logger()
        {
        }

        public void Trace(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogTrace(message, args);
        }

        public void Trace(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Trace(callerName, message, args);
        }

        public void Debug(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogDebug(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Debug(callerName, message, args);
        }

        public void Info(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogInformation(message, args);
        }

        public void Info(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Info(callerName, message, args);
        }

        public void Warn(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogWarning(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Warn(callerName, message, args);
        }

        public void Warn(string tag, Exception exp)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogWarning(exp, string.Empty);
        }

        public void Warn(Exception exp)
        {
            string callerName = GetCallerName(1);
            Warn(callerName, exp);
        }

        public void Error(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogError(message, args);
        }

        public void Error(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Error(callerName, message, args);
        }

        public void Error(string tag, Exception exp)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogError(exp, string.Empty);
        }

        public void Error(Exception exp)
        {
            string callerName = GetCallerName(1);
            Error(callerName, exp);
        }

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Fatal(string tag, string message, params object[] args)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogCritical(message, args);
        }

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Fatal(string message, params object[] args)
        {
            string callerName = GetCallerName(1);
            Fatal(callerName, message, args);
        }

        public void Fatal(string tag, Exception exp)
        {
            _CheckInit();
            ILogger logger = _GetLogger(tag);
            logger.LogCritical(exp, string.Empty);
        }

        public void Fatal(Exception exp)
        {
            string callerName = GetCallerName(1);
            Fatal(callerName, exp);
        }
    }
}
