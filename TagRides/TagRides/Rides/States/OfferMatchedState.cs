using TagRides.Services;

namespace TagRides.Rides.States
{
    public class OfferMatchedState : AbstractMatchedState
    {
        public OfferMatchedState(Ridesharing.StateBase previous, IMatchedRideRelatedRequest match)
            : base(previous, match)
        {
        }
    }
}
