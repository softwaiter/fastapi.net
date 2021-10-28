using CodeM.Common.Ioc;
using CodeM.FastApi.System.Core;
using System.Text.RegularExpressions;

namespace CodeM.FastApi.Controllers
{
    public class BaseController
    {
        private static Regex sSuffixControler = new Regex("Controller$");
        private static Regex sLastDot = new Regex("\\.[^\\.]*$");

        public dynamic Service(bool singleton = true)
        {
            string controllerFullName = GetType().FullName;
            string serviceFullName = controllerFullName.Replace(".Controllers.", ".Services.");
            serviceFullName = sSuffixControler.Replace(serviceFullName, "Service");
            if (singleton)
            {
                return IocUtils.GetSingleObject(serviceFullName);
            }
            else
            {
                return IocUtils.GetObject(serviceFullName);
            }
        }

        public dynamic Service(string serviceName, bool singleton = true)
        {
            string controllerFullName = GetType().FullName;
            string serviceFullName = controllerFullName.Replace(".Controllers.", ".Services.");
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
