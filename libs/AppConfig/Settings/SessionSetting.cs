using CodeM.Common.Tools;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace CodeM.FastApi.Config.Settings
{
    public class SessionSetting
    {
        public class SessionSettingCookie
        {
            private string mName = "fastapi.sid";
            public string Name 
            {
                get 
                {
                    return mName;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new Exception("Session的Cookie名称不能为空。");
                    }
                    mName = value;
                }
            }

            public bool HttpOnly { get; set; } = true;

            public SameSiteMode SameSite { get; set; } = SameSiteMode.None;

            public CookieSecurePolicy Secure { get; set; } = CookieSecurePolicy.None;

            private string mMaxAge = null;
            /// <summary>
            /// cookie有效期，默认单位：秒，可随值指定单位，ms-毫秒，s-秒，m-分钟，h-小时，d-天
            /// </summary>
            public string MaxAge 
            {
                get
                {
                    return mMaxAge;
                }
                set
                {
                    DateTimeUtils.CheckStringTimeSpan(value);
                    mMaxAge = value;
                }
            }

            public TimeSpan? MaxAgeTimeSpan
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(mMaxAge))
                    {
                        return DateTimeUtils.GetTimeSpanFromString(mMaxAge);
                    }
                    return null;
                }
            }
        }

        public class SessionSettingRedis
        {
            public bool Enable { get; set; } = false;

            private string mHost = "127.0.0.1";
            public string Host 
            {
                get
                {
                    return mHost;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(mHost))
                    {
                        throw new Exception("Session的Redis配置项Host不能为空。");
                    }
                    mHost = value;
                }
            }

            public int Port { get; set; } = 6379;

            public int Database = 0;

            public string Password = null;

            public int Retry = 3;

            public int Timeout = 5000;

            public bool Ssl = false;

            private string mSslHost = null;
            public string SslHost
            {
                get
                {
                    return mSslHost;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(mSslHost))
                    {
                        throw new Exception("Session的Redis配置项SslHost不能为空。");
                    }
                    mSslHost = value;
                }
            }

            private string mSslProtocols = null;
            public string SslProtocols 
            {
                get
                {
                    return mSslProtocols;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(mSslProtocols))
                    {
                        throw new Exception("Session的Redis配置项SslProtocols不能为空。");
                    }
                    mSslProtocols = value;
                }
            }

            private string mInstanceName = "fastapi-";
            public string InstanceName
            {
                get
                {
                    return mInstanceName;
                }
                set
                {
                    if (string.IsNullOrWhiteSpace(mInstanceName))
                    {
                        throw new Exception("Session的Redis配置项InstanceName不能为空。");
                    }
                    mInstanceName = value;
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Host).Append(":").Append(Port)
                    .Append(",connectRetry=").Append(Retry)
                    .Append(",connectTimeout=").Append(Timeout)
                    .Append(", defaultDatabase=").Append(Database);
                if (Password != null)
                {
                    sb.Append(",password=").Append(Password);
                }
                if (Ssl)
                {
                    sb.Append(",ssl=").Append(Ssl);
                    if (!string.IsNullOrWhiteSpace(SslHost))
                    {
                        sb.Append(",sslHost=").Append(SslHost);
                    }
                    if (!string.IsNullOrWhiteSpace(SslProtocols))
                    {
                        sb.Append(",sslProtocols=").Append(SslProtocols);
                    }
                }
                return sb.ToString();
            }
        }

        public bool Enable { get; set; } = true;

        private string mTimeout = "20m";
        /// <summary>
        /// session超时时间，默认单位：秒，可随值指定单位，ms-毫秒，s-秒，m-分钟，h-小时，d-天
        /// </summary>
        public string Timeout
        {
            get
            {
                return mTimeout;
            }
            set
            {
                DateTimeUtils.CheckStringTimeSpan(mTimeout);
                mTimeout = value;
            }
        }

        public TimeSpan TimeoutTimeSpan
        {
            get
            {
                TimeSpan? ts = DateTimeUtils.GetTimeSpanFromString(mTimeout, false);
                return ts != null ? ts.Value : new TimeSpan();
            }
        }

        public SessionSettingCookie Cookie
        {
            get;
            set;
        } = new SessionSettingCookie();

        public SessionSettingRedis Redis
        {
            get;
            set;
        } = new SessionSettingRedis();

    }
}
