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
                code = 0,   //code默认为0，代表成功
                data = _data,
                error = string.Empty
            });
            await cc.Response.WriteAsync(result);
        }

        public static async Task JsonAsync(this ControllerContext cc, Exception _exp)
        {
            CheckContentType(cc.Response);

            string result = JsonFormatter.SerializeObject(new
            {
                code = -1,  //code默认为-1，代表出错
                error = _exp.Message
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
