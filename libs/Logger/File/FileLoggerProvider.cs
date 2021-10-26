using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CodeM.FastApi.Log.File
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider, IDisposable
    {
        private IConfigurationSection mOptions;
        private ConcurrentDictionary<string, ILogger> mLoggers = new ConcurrentDictionary<string, ILogger>();

        public FileLoggerProvider(IConfigurationSection options)
        {
            mOptions = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return mLoggers.GetOrAdd(categoryName, (key) =>
            {
                return new FileLogger(mOptions, key);
            });
        }

        public void Dispose()
        {
            mLoggers.Clear();
            mLoggers = null;
        }
    }
}
