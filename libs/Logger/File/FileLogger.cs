using Microsoft.Extensions.Logging;
using System;

namespace CodeM.FastApi.Logger.File
{
    public class FileLogger : ILogger, IDisposable
    {
        public FileLogger(string categoryName)
        {
            this.CategoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public string CategoryName { get; set; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var result = $"{ GetLogLevelString(logLevel) }: {this.CategoryName}[{eventId}] - {DateTime.Now}{Environment.NewLine}";
            
            var message = formatter(state, null);
            if (!string.IsNullOrWhiteSpace(message))
            {
                result += ("      " + message).Replace(Environment.NewLine, Environment.NewLine + "      ");
            }

            if (exception != null)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    result += Environment.NewLine;
                }
                result += ("      " + exception).Replace(Environment.NewLine, Environment.NewLine + "      ");
            }

            FileWriter.Write(result);
        }
        private string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Dispose()
        {
        }
    }
}
