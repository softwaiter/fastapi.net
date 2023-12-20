using CodeM.Common.Ioc;
using CodeM.FastApi.System.Core;

namespace CodeM.FastApi.Controllers
{
    public class BaseController
    {
        public T Service<T>(bool singleton = true)
        {
            string serviceFullName = typeof(T).FullName;
            if (singleton)
            {
                return (T)Wukong.GetSingleObject(serviceFullName);
            }
            else
            {
                return (T)Wukong.GetObject(serviceFullName);
            }
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
