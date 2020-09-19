using Microsoft.AspNetCore.Http;
using Swifter.Json;
using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Context
{
    public static class JsonResponse
    {

        private static void CheckContentType(HttpResponse response)
        {
            if (!response.HasStarted)
            {
                response.ContentType = "application/json";
            }
        }

        public static async Task JsonAsync(this ControllerContext cc, object _data = null)
        {
            CheckContentType(cc.Response);

            string result = JsonFormatter.SerializeObject(new
            {
                code = _data is Exception ? -1 : 0,   //0，成功；-1：失败
                data = _data is Exception ? null : _data,
                error = _data is Exception ? (_data as Exception).Message : null
            });
            await cc.Response.WriteAsync(result);
        }

        public static async Task JsonAsync(this ControllerContext cc, int _code, object _data = null, string _error = null)
        {
            CheckContentType(cc.Response);

            string result = JsonFormatter.SerializeObject(new
            {
                code = _code,
                data = _data,
                error = _error
            });
            await cc.Response.WriteAsync(result);
        }

    }
}
