using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controller
{
    public class User
    {

        public async Task Handle(ControllerContext cc)
        {
            await cc.Json("Hello World.");
        }

    }
}
