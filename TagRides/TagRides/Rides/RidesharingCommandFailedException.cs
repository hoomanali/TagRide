using System;
namespace TagRides.Rides
{
    public class RidesharingCommandFailedException : ApplicationException
    {
        public RidesharingCommandFailedException()
        {
        }

        public RidesharingCommandFailedException(string message) : base(message)
        {
        }

        public RidesharingCommandFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
