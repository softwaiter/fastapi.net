﻿using CodeM.Common.Tools.Json;
using CodeM.FastApi.Config;
using CodeM.FastApi.Context.Params;
using CodeM.FastApi.Context.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.IO;
using System.Threading.Tasks;

namespace CodeM.FastApi.Context
{
    public class ControllerContext
    {
        private HttpContext mContext;

        private HeaderParams mHeaders = null;
        private RouteParams mRouteParams = null;
        private QueryParams mQueryParams = null;
        private string mPostContent = null;
        private PostForms mPostForms = null;
        private dynamic mPostJson = null;
        private CookieWrapper mCookies = null;

        public static ControllerContext FromHttpContext(HttpContext context, ApplicationConfig config)
        {
            return new ControllerContext(context, config);
        }

        public ControllerContext(HttpContext context, ApplicationConfig config)
        {
            mContext = context;

            if (mContext != null)
            {
                mContext.Request.EnableBuffering();
            }

            if (config != null)
            {
                Config = config;
            }
        }

        private SessionWrapper mSession;
        public SessionWrapper Session
        {
            get
            {
                if (Config.Session.Enable && mSession == null)
                {
                    mSession = new SessionWrapper(mContext);
                }
                return mSession;
            }
        }

        public HttpRequest Request
        {
            get
            {
                return mContext.Request;
            }
        }

        public HttpResponse Response
        {
            get
            {
                return mContext.Response;
            }
        }

        public int State
        {
            get
            {
                return mContext.Response.StatusCode;
            }
            set
            {
                mContext.Response.StatusCode = value;
            }
        }

        public HeaderParams Headers
        {
            get
            {
                if (mHeaders == null && mContext != null)
                {
                    mHeaders = new HeaderParams(mContext.Request.Headers);
                }
                return mHeaders;
            }
        }

        public RouteParams RouteParams
{
            get
            {
                if (mRouteParams == null && mContext != null)
                {
                    mRouteParams = new RouteParams(mContext.GetRouteData());
                }
                return mRouteParams;
            }
        }

        public QueryParams QueryParams
        {
            get
            {
                if (mQueryParams == null && mContext != null)
                {
                    mQueryParams = new QueryParams(mContext.Request.Query);
                }
                return mQueryParams;
            }
        }

        public string PostContent
        {
            get
            {
                if (mPostContent == null && mContext != null && mContext.Request != null)
                {
                    if (mContext.Request.Body != null && mContext.Request.ContentLength > 0)
                    {
                        mContext.Request.Body.Seek(0, SeekOrigin.Begin);

                        StreamReader sr = new StreamReader(mContext.Request.Body);
                        Task<string> getContentTask = sr.ReadToEndAsync();
                        Task.WaitAll(getContentTask);

                        mContext.Request.Body.Seek(0, SeekOrigin.Begin);

                        mPostContent = getContentTask.Result;
                    }
                }
                return mPostContent;
            }
        }

        public PostForms PostForms
        {
            get
            {
                if (mPostForms == null && mContext != null)
                {
                    if (mContext.Request != null && mContext.Request.HasFormContentType)
                    {
                        mPostForms = new PostForms(mContext.Request.Form);
                    }
                }
                return mPostForms;
            }
        }

        public dynamic PostJson
        {
            get
            {
                if (mPostJson == null && mContext != null)
                {
                    if (mContext.Request != null && ("" + mContext.Request.ContentType).ToLower().Contains("json"))
                    {
                        if (!string.IsNullOrEmpty(PostContent))
                        {
                            mPostJson = new Json2DynamicParser().Parse(PostContent);
                        }
                    }
                }
                return mPostJson;
            }
        }

        public CookieWrapper Cookies
        {
            get
            {
                if (mCookies == null && mContext != null)
                {
                    mCookies = new CookieWrapper(mContext, Config.Cookie.Keys);
                }
                return mCookies;
            }
        }

        public ApplicationConfig Config
        {
            get;
        }

        public string Ip
        {
            get
            {
                string result = null;

                StringValues forwards = mContext.Request.Headers["X-Forwarded-For"];
                if (forwards.Count > 0)
                {
                    result = forwards[0];
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = mContext.Connection.RemoteIpAddress.ToString();
                }

                if (result.StartsWith("::ffff:"))
                {
                    result = result.Substring(7);
                }

                return result;
            }
        }

    }
}
