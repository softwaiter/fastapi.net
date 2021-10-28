namespace CodeM.FastApi.Services
{
    public class HelloService : BaseService
    {
        public string GetHi()
        {
            return "你好，世界！";
        }
    }
}
