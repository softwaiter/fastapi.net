using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {

        public async Task Handle(ControllerContext cc)
        {
            await cc.JsonAsync("Hello World.");
        }

        public async Task List(ControllerContext cc)
        {
            await cc.JsonAsync("这是一个列表");
        }
        public static async Task Request(ControllerContext cc)
        {
            await cc.JsonAsync("这是一个列表");
        }

    }
}
