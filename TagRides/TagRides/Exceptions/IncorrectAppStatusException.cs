using System;
namespace TagRides.Exceptions
{
    /// <summary>
    /// An exception thrown when an action is attempted while the app is in not
    /// in the right state.
    /// </summary>
    public class IncorrectAppStatusException : ApplicationException
    {
        public IncorrectAppStatusException()
        {
        }

        public IncorrectAppStatusException(string message)
            : base(message)
        {
        }

        public IncorrectAppStatusException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
