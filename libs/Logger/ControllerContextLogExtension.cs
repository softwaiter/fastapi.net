using CodeM.FastApi.Log;
using System;

namespace CodeM.FastApi.Context
{
    public static class ControllerContextLogExtension
    {

        public static void Trace(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Trace(callerName, message, args);
        }

        public static void Debug(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Debug(callerName, message, args);
        }

        public static void Info(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Info(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Warn(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Warn(callerName, exp);
        }

        public static void Error(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Error(callerName, message, args);
        }

        public static void Error(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Error(callerName, exp);
        }

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Fatal(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Fatal(callerName, message, args);
        }

        public static void Fatal(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.GetInstance().Fatal(callerName, exp);
        }

    }
}
