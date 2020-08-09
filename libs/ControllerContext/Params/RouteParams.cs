using Microsoft.AspNetCore.Routing;

namespace CodeM.FastApi.Context.Params
{
    public class RouteParams
    {
        private RouteData mData;

        public RouteParams(RouteData data)
        {
            mData = data;
        }

        public string this[string key]
        {
            get
            {
                if (mData != null)
                {
                    object value;
                    if (mData.Values.TryGetValue(key, out value))
                    {
                        return value.ToString();
                    }
                }
                return null;
            }
        }
    }
}
