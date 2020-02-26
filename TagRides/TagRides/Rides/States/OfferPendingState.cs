using TagRides.Services;
using TagRides.Shared.RideData;

namespace TagRides.Rides.States
{
    public class OfferPendingState : AbstractPendingState
    {
        public RideOffer Offer { get; }

        public OfferPendingState(Ridesharing.StateBase previous, IPendingRideRelatedRequest response, RideOffer offer)
            : base(previous, response)
        {
            Offer = offer;
        }

        protected override void HandleMatched(IMatchedRideRelatedRequest match)
        {
            TransitionTo(new OfferMatchedState(this, match));
        }
    }
}
