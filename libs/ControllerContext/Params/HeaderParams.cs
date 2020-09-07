using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeM.FastApi.Context.Params
{
    public class HeaderParams
    {

        IHeaderDictionary mData;

        public HeaderParams(IHeaderDictionary data)
        {
            mData = data;
        }

        public string this[string key]
        {
            get
            {
                if (mData != null)
                {
                    StringValues result;
                    if (mData.TryGetValue(key, out result))
                    {
                        return string.Join(",", result);
                    }
                }
                return null;
            }
        }

        public string this[int index]
        {
            get
            {
                if (mData != null)
                {
                    if (index >= 0 && index < mData.Keys.Count)
                    {
                        string[] keys = new string[mData.Keys.Count];
                        mData.Keys.CopyTo(keys, 0);
                        string key = keys[index];
                        StringValues result;
                        if (mData.TryGetValue(key, out result))
                        {
                            return string.Join(",", result);
                        }
                    }
                }
                return null;
            }
        }

        public bool ContainsKey(string key)
        {
            if (mData != null)
            {
                return mData.ContainsKey(key);
            }
            return false;
        }

        public string Get(string key, string defaultValue)
        {
            if (ContainsKey(key))
            {
                return this[key];
            }
            return defaultValue;
        }

        public string Get(int index, string defaultValue)
        {
            if (mData != null)
            {
                if (index >= 0 && index < mData.Keys.Count)
                {
                    return this[index];
                }
            }
            return defaultValue;
        }

        public int Count
        {
            get
            {
                if (mData != null)
                {
                    return mData.Count;
                }
                return 0;
            }
        }

    }
}
