using Microsoft.AspNetCore.Http;
using Swifter.Json;
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

        public static async Task Json(this ControllerContext cc, object _data = null)
        {
            CheckContentType(cc.Response);

            string result = JsonFormatter.SerializeObject(new
            {
                code = 0,   //code默认为0，代表成功
                data = _data
            });
            await cc.Response.WriteAsync(result);
        }

        public static async Task Json(this ControllerContext cc, int _code, object _data= null)
        {
            CheckContentType(cc.Response);

            string result = JsonFormatter.SerializeObject(new
            {
                code = _code,
                data = _data
            });
            await cc.Response.WriteAsync(result);
        }

    }
}
