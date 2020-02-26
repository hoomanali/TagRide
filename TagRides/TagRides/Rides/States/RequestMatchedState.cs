using TagRides.Services;

namespace TagRides.Rides.States
{
    public class RequestMatchedState : AbstractMatchedState
    {
        public RequestMatchedState(Ridesharing.StateBase previous, IMatchedRideRelatedRequest match)
            : base(previous, match)
        {
        }
    }
}
