using System;
using System.Threading.Tasks;

namespace CodeM.FastApi.Common
{
    public static class Utils
    {
        private static bool mIsDevelopmentChecked = false;
        private static bool mIsDevelopment = false;
        public static async Task<bool> IsDevelopment()
        {
            if (!mIsDevelopmentChecked)
            {
                string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                mIsDevelopment = "Development".Equals(env, StringComparison.OrdinalIgnoreCase);
                mIsDevelopmentChecked = true;
            }
            await Task.CompletedTask;
            return mIsDevelopment;
        }
    }
}
