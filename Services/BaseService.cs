using CodeM.FastApi.System.Core;

namespace CodeM.FastApi.Services
{
    public class BaseService
    {
        public dynamic Service(string serviceName, bool singleton = true)
        {
            return Application.Instance().Service(serviceName, singleton);
        }

        public Application App
        {
            get
            {
                return Application.Instance();
            }
        }
    }
}
