using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controllers
{
    public class HelloController : BaseController
    {

        public async Task Handle(ControllerContext cc)
        {
            dynamic helloService = Service("Hello");
            string hi = helloService.GetHi();
            await cc.JsonAsync(hi);
        }

    }
}
