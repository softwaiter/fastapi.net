using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CodeM.FastApi.Logger.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider, IDisposable
    {
        private ConcurrentDictionary<string, ILogger> mLoggers = new ConcurrentDictionary<string, ILogger>();

        public ILogger CreateLogger(string categoryName)
        {
            return mLoggers.GetOrAdd(categoryName, (key) =>
            {
                return new FileLogger(key);
            });
        }

        public void Dispose()
        {
            mLoggers.Clear();
            mLoggers = null;
        }
    }
}
