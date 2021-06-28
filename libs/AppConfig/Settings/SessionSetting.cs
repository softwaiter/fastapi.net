using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace CodeM.FastApi.Config.Settings
{
    public class SessionSetting
    {
        private static bool _CheckTimeValue(string time)
        {
            bool result = true;

            if (!string.IsNullOrWhiteSpace(time))
            {
                int value;
                string trimedTime = time.Trim().ToLower();
                if (trimedTime.EndsWith("ms"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 2);
                    result = int.TryParse(trimedTime, out value);
                }
                else if (trimedTime.EndsWith("s") || trimedTime.EndsWith("m") ||
                    trimedTime.EndsWith("h") || trimedTime.EndsWith("d"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 1);
                    result = int.TryParse(trimedTime, out value);
                }
                else
                {
                    result = int.TryParse(trimedTime, out value);
                }

                if (!result)
                {
                    throw new Exception(string.Concat("错误的有效期格式，单位仅支持ms、s、m、h、d：", time));
                }
                else
                {
                    if (value < 0)
                    {
                        throw new Exception(string.Concat("有效期时间必须大于等于0：", time));
                    }
                }
            }
            else
            {
                throw new Exception("有效期不能设置空值。");
            }

            return result;
        }

        private static TimeSpan? _GetTimeSpanFromString(string time)
        {
            TimeSpan? result = null;

            if (!string.IsNullOrWhiteSpace(time))
            {
                int value;
                string trimedTime = time.Trim().ToLower();
                if (trimedTime.EndsWith("ms"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 2);
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromMilliseconds(value);
                    }
                }
                else if (trimedTime.EndsWith("s"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 1);
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromSeconds(value);
                    }
                }
                else if (trimedTime.EndsWith("m"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 1);
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromMinutes(value);
                    }
                }
                else if (trimedTime.EndsWith("h"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 1);
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromHours(value);
                    }
                }
                else if (trimedTime.EndsWith("d"))
                {
                    trimedTime = trimedTime.Substring(0, trimedTime.Length - 1);
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromDays(value);
                    }
                }
                else
                {
                    if (int.TryParse(trimedTime, out value))
                    {
                        result = TimeSpan.FromSeconds(value);
                    }
                }
            }

            return result;
        }

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
                    _CheckTimeValue(value);
                    mMaxAge = value;
                }
            }

            public TimeSpan? MaxAgeTimeSpan
            {
                get
                {
                    return _GetTimeSpanFromString(mMaxAge);
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
                _CheckTimeValue(mTimeout);
                mTimeout = value;
            }
        }

        public TimeSpan TimeoutTimeSpan
        {
            get
            {
                return (TimeSpan)_GetTimeSpanFromString(mTimeout);
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
