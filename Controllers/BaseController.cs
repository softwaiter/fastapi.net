using CodeM.FastApi.System.Core;
using CodeM.FastApi.Context;

namespace CodeM.FastApi.Controllers
{
    public class BaseController
    {
        ControllerContext mabc;

        public App App
        {
            get
            {
                return App.Create();
            }
        }
    }
}
