using CodeM.FastApi.Log.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Logging
{
    public static class FileLoggerFactoryExtensions
    {
        private static FileLoggerProvider sFileLoggerProvider;
        private static object sFileLoggerProviderLock = new object();

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, IConfigurationSection options)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>((spes) =>
            {
                if (sFileLoggerProvider == null)
                {
                    lock (sFileLoggerProviderLock)
                    {
                        if (sFileLoggerProvider == null)
                        {
                            sFileLoggerProvider = new FileLoggerProvider(options);
                        }
                    }
                }
                return sFileLoggerProvider;
            }));
            return builder;
        }
    }
}
