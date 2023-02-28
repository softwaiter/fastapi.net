using CodeM.Common.Tools;
using Microsoft.AspNetCore.Http;

namespace CodeM.FastApi.Context.Wrappers
{
    public class CookieWrapper
    {
        private HttpContext mContext;
        private string mKeys = null;

        public CookieWrapper(HttpContext context, string keys = null)
        {
            mContext = context;
            mKeys = keys;
        }

        public void Set(string key, string value, CookieOptionsExt options = null)
        {
            if (mContext != null && mContext.Response != null &&
                mContext.Response.Cookies != null)
            {
                if (options != null)
                {
                    if (options.Encrypt && !string.IsNullOrWhiteSpace(mKeys))
                    {
                        string[] keyItems = mKeys.Split(",");
                        if (keyItems.Length > 0)
                        {
                            value = Xmtool.Crypto().AESEncode(value, keyItems[0].Trim());
                        }
                    }
                    mContext.Response.Cookies.Append(key, value, options);
                }
                else
                {
                    mContext.Response.Cookies.Append(key, value);
                }
            }
        }

        public string Get(string key, CookieOptionsExt options = null)
        {
            if (mContext != null && mContext.Request != null &&
                mContext.Request.Cookies != null && 
                mContext.Request.Cookies.ContainsKey(key))
            {
                string value;
                if (mContext.Request.Cookies.TryGetValue(key, out value))
                {
                    if (options != null)
                    {
                        if (options.Encrypt && !string.IsNullOrWhiteSpace(mKeys))
                        {
                            string[] keyItems = mKeys.Split(",");
                            foreach (string keyItem in keyItems)
                            {
                                try
                                {
                                    value = Xmtool.Crypto().AESDecode(value, keyItem.Trim());
                                    break;
                                }
                                catch
                                {
                                    ;
                                }
                            }
                        }
                    }
                    return value;
                }
            }
            return null;
        }

        public void Delete(string key)
        {
            if (mContext != null && mContext.Response != null &&
                mContext.Response.Cookies != null)
            {
                mContext.Response.Cookies.Delete(key);
            }
        }

    }
}
