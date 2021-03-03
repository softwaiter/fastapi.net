using Microsoft.AspNetCore.Http;
using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeM.FastApi.Context
{
    public static class JsonResponse
    {
        private static JsonSerializerOptions sJsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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

            string result = JsonSerializer.Serialize<dynamic>(new
            {
                code = _data is Exception ? -1 : 0,   //0，成功；-1：失败
                data = _data is Exception ? null : _data,
                error = _data is Exception ? (_data as Exception).Message : null
            }, sJsonSerializerOptions);
            await cc.Response.WriteAsync(result);
        }

        public static async Task JsonAsync(this ControllerContext cc, int _code, object _data = null, string _error = null)
        {
            CheckContentType(cc.Response);

            string result = JsonSerializer.Serialize<dynamic>(new
            {
                code = _code,
                data = _data,
                error = _error
            }, sJsonSerializerOptions);
            await cc.Response.WriteAsync(result);
        }

    }
}
