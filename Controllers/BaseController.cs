using CodeM.FastApi.System.Core;

namespace CodeM.FastApi.Controllers
{
    public class BaseController
    {
        public App App
        {
            get
            {
                return App.Create();
            }
        }
    }
}
