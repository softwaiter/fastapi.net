using CodeM.FastApi.Logger;
using System;

namespace CodeM.FastApi.Context
{
    public static class ControllerContextLogExtension
    {

        public static void Trace(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Trace(callerName, message, args);
        }

        public static void Debug(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Debug(callerName, message, args);
        }

        public static void Info(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Info(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Warn(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, Exception exp)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Warn(callerName, exp);
        }

        public static void Error(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Error(callerName, message, args);
        }

        public static void Error(this ControllerContext cc, Exception exp)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Error(callerName, exp);
        }

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Fatal(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Fatal(callerName, message, args);
        }

        public static void Fatal(this ControllerContext cc, Exception exp)
        {
            string callerName = LogUtils.GetCallerName(1);
            LogUtils.Fatal(callerName, exp);
        }

    }
}
