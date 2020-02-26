using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Shared.RideData;

namespace TagRides.Rides.States
{
    /// <summary>
    /// Default state. The user has no pending offers or requests and is
    /// not participating in any ride.
    /// </summary>
    public class NoneState : Ridesharing.StateBase
    {
        public NoneState(Ridesharing ridesharing)
            : base(ridesharing)
        {
        }

        public NoneState(Ridesharing.StateBase previous)
            : base(previous)
        {
        }

        /// <summary>
        /// Posts the offer.
        /// </summary>
        /// <returns>True if posting succeeded, false otherwise.</returns>
        /// <param name="offer">Offer to post.</param>
        public async Task<bool> PostOffer(RideOffer offer)
        {
            if (IsStale)
                return false;

            IPendingRideRelatedRequest response = await RideOfferer.SubmitRideOffer(offer);

            if (response != null)
            {
                TransitionTo(new OfferPendingState(this, response, offer));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Posts the request.
        /// </summary>
        /// <returns>True if posting succeeded, false otherwise.</returns>
        /// <param name="request">Request to post.</param>
        public async Task<bool> PostRequest(RideRequest request)
        {
            if (IsStale)
                return false;

            IPendingRideRelatedRequest response = await RideRequester.SubmitRideRequest(request);

            if (response != null)
            {
                TransitionTo(new RequestPendingState(this, response, request));
                return true;
            }

            return false;
        }
    }
}
