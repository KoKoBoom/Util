using System;
using System.Runtime.Serialization;

namespace UtilTool
{
    /// <summary>
    /// 异常扩展
    /// </summary>
    [Serializable]
    public class ExceptionExtensions : ApplicationException
    {
        public ApiStatusCode? ErrorCode { get; private set; } = null;

        public ExceptionExtensions()
        {
        }

        public ExceptionExtensions(string message, ApiStatusCode? errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ExceptionExtensions(string message) : base(message)
        {
        }

        public ExceptionExtensions(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExceptionExtensions(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}