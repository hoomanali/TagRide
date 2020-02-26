using TagRides.Services;
using TagRides.Shared.RideData;
namespace TagRides.Rides.States
{
    public class RequestPendingState : AbstractPendingState
    {
        public RideRequest Request { get; }

        public RequestPendingState(Ridesharing.StateBase previous, IPendingRideRelatedRequest response, RideRequest request)
            : base(previous, response)
        {
            Request = request;
        }

        protected override void HandleMatched(IMatchedRideRelatedRequest match)
        {
            TransitionTo(new RequestMatchedState(this, match));
        }
    }
}
