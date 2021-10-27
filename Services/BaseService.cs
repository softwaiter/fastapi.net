using CodeM.FastApi.System.Core;

namespace CodeM.FastApi.Services
{
    public class BaseService
    {
        public App App
        {
            get
            {
                return App.GetInstance();
            }
        }
    }
}
