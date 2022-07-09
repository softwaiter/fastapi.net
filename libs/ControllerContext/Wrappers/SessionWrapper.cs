using Microsoft.AspNetCore.Http;

namespace CodeM.FastApi.Context.Wrappers
{
    public class SessionWrapper
    {
        private HttpContext mContext;
        public SessionWrapper(HttpContext context)
        {
            mContext = context;
        }

        public string Id
        {
            get
            {
                return mContext.Session.Id;
            }
        }

        public void SetString(string key, string value)
        {
            mContext.Session.SetString(key, value);
        }

        public string GetString(string key)
        {
            return mContext.Session.GetString(key);
        }

        public void SetInt32(string key, int value)
        {
            mContext.Session.SetInt32(key, value);
        }

        public int? GetInt32(string key)
        {
            return mContext.Session.GetInt32(key);
        }

        public void SetBoolean(string key, bool value)
        {
            SetInt32(key, value ? 1 : 0);
        }

        public bool GetBoolean(string key)
        {
            return GetInt32(key) == 1;
        }

        public void Clear()
        {
            mContext.Session.Clear();
        }

        public void Remove(string key)
        { 
            mContext.Session.Remove(key);
        }
    }
}
