using System;
using System.Runtime.Serialization;

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

        public FastApiException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
