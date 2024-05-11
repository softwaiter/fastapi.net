using System;

namespace CodeM.FastApi.Common
{
    public class FastApiException : Exception
    {
        public FastApiException()
            : base()
        {
        }

        public FastApiException(string message)
            : base(message)
        {
        }

        public FastApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
