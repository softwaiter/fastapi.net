using CodeM.FastApi.Logger.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Logging
{
    public static class FileLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
            return builder;
        }
    }
}
