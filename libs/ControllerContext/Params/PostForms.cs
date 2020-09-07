using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CodeM.FastApi.Context.Params
{
    public class PostForms
    {
        IFormCollection mForm;

        public PostForms(IFormCollection form)
        {
            mForm = form;
        }

        public string this[string key]
        {
            get
            {
                if (mForm != null)
                {
                    StringValues result;
                    if (mForm.TryGetValue(key, out result))
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
                if (mForm != null)
                {
                    if (index >= 0 && index < mForm.Count)
                    {
                        string[] keys = new string[mForm.Count];
                        mForm.Keys.CopyTo(keys, 0);
                        string key = keys[index];
                        StringValues result;
                        if (mForm.TryGetValue(key, out result))
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
            if (mForm != null)
            {
                return mForm.ContainsKey(key);
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
            if (mForm != null)
            {
                if (index >= 0 && index < mForm.Count)
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
                if (mForm != null)
                {
                    return mForm.Count;
                }
                return 0;
            }
        }

    }
}
