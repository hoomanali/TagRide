using System;
namespace TagRides.Server.Exceptions
{
    /// <summary>
    /// This exception represents a failed access to an external API. This can
    /// mean that the API returned an error message, or that the API returned
    /// an unexpected value.
    /// </summary>
    public class ApiAccessException : ApplicationException
    {
        public ApiAccessException()
        {
        }

        public ApiAccessException(string message)
            : base(message)
        {
        }

        public ApiAccessException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
