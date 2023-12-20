using CodeM.FastApi.System.Core;

namespace CodeM.FastApi.Services
{
    public class BaseService
    {
        public T Service<T>(bool singleton = true)
        {
            return Application.Instance().Service<T>(singleton);
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
