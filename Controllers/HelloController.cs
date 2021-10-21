using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controllers
{
    public class HelloController : BaseController
    {

        public async Task Handle(ControllerContext cc)
        {
            await cc.JsonAsync("Hello World.");
        }

    }
}
