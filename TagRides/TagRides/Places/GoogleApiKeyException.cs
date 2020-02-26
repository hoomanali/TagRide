using System;
namespace TagRides.Places
{
    public class GoogleApiKeyException : ApplicationException
    {
        public GoogleApiKeyException()
        {
        }

        public GoogleApiKeyException(string message)
            : base(message)
        {
        }

        public GoogleApiKeyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
