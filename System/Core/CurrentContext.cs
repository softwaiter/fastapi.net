using CodeM.FastApi.Context;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Threading;

namespace CodeM.FastApi.System.Core
{
    public class CurrentContext
    {
        private static ConcurrentDictionary<int, HttpContext> sRequests = new ConcurrentDictionary<int, HttpContext>();

        public static void Add(HttpContext context)
        {
            int procId = Thread.GetCurrentProcessorId();
            sRequests.TryAdd(procId, context);
        }

        public static ControllerContext Context
        {
            get
            {
                HttpContext context = null;
                int procId = Thread.GetCurrentProcessorId();
                if (sRequests.TryGetValue(procId, out context))
                {
                    return ControllerContext.FromHttpContext(context,
                        Application.Instance().Config());
                }
                return null;
            }
        }

        public static void Remove()
        {
            HttpContext context;
            int procId = Thread.GetCurrentProcessorId();
            sRequests.TryRemove(procId, out context);
        }
    }
}
