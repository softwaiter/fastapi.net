using CodeM.FastApi.System.Core;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.Services
{
    public class BaseService
    {
        public dynamic Service(string serviceName, bool singleton = true)
        {
            return App.GetInstance().Service(serviceName, singleton);
        }

        public App App
        {
            get
            {
                return App.GetInstance();
            }
        }
    }
}
