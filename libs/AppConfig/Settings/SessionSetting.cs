using Microsoft.AspNetCore.Http;
using System;

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
            public string Name { get; set; } = "fastapi.sid";

            public bool HttpOnly { get; set; } = true;

            public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

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

        public class SessionSettingOptions
        {
            private string mTimeout = "20m";
            /// <summary>
            /// session超时时间，默认单位：秒，可随值指定单位，ms-毫秒，s-秒，m-分钟，h-小时，d-天
            /// </summary>
            public string Timeout {
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
        }

        public bool Enable { get; set; } = true;

        public SessionSettingOptions Options
        {
            get;
            set;
        } = new SessionSettingOptions();
    }
}
