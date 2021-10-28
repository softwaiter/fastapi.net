using CodeM.Common.Ioc;
using CodeM.FastApi.System.Core;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.Services
{
    public class BaseService
    {
        private static Regex sLastDot = new Regex("\\.[^\\.]*$");

        public dynamic Service(string serviceName, bool singleton = true)
        {
            string serviceFullName = GetType().FullName;
            serviceFullName = sLastDot.Replace(serviceFullName, string.Concat(".", serviceName, "Service"));
            if (singleton)
            {
                return IocUtils.GetSingleObject(serviceFullName);
            }
            else
            {
                return IocUtils.GetObject(serviceFullName);
            }
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
