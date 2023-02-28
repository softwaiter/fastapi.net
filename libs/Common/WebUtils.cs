using Microsoft.AspNetCore.Http;
using System;

namespace CodeM.FastApi.Common
{
    public class WebUtils
    {
        public static string GetClientIp(HttpRequest request)
        {
            string unknown = "";
            string ip = request.Headers["x-forwarded-for"];

            if (ip == null || string.IsNullOrWhiteSpace(ip) ||
                unknown.Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ip = request.Headers["Proxy-Client-IP"];
            }

            if (ip == null || string.IsNullOrWhiteSpace(ip) ||
                unknown.Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ip = request.Headers["WL-Proxy-Client-IP"];
            }

            if (ip == null || string.IsNullOrWhiteSpace(ip) ||
                unknown.Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ip = request.Headers["HTTP_CLIENT_IP"];
            }

            if (ip == null || string.IsNullOrWhiteSpace(ip) ||
                unknown.Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ip = request.Headers["HTTP_X_FORWARDED_FOR"];
            }

            if (ip == null || string.IsNullOrWhiteSpace(ip) ||
                unknown.Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ip = request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }

            return ip;
        }
    }
}
