﻿using CodeM.FastApi.Log;
using System;

namespace CodeM.FastApi.Context
{
    public static class ControllerContextLogExtension
    {

        public static void Trace(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Trace(callerName, message, args);
        }

        public static void Debug(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Debug(callerName, message, args);
        }

        public static void Info(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Info(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Warn(callerName, message, args);
        }

        public static void Warn(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Warn(callerName, exp);
        }

        public static void Error(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Error(callerName, message, args);
        }

        public static void Error(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Error(callerName, exp);
        }

        /// <summary>
        /// 致命错误
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Fatal(this ControllerContext cc, string message, params object[] args)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Fatal(callerName, message, args);
        }

        public static void Fatal(this ControllerContext cc, Exception exp)
        {
            string callerName = Logger.GetCallerName(1);
            Logger.Create().Fatal(callerName, exp);
        }

    }
}
