using System.Threading.Tasks;
using TagRides.Rides;
using TagRides.Shared.RideData;

namespace TagRides.Services
{
    /// <summary>
    /// Implementation of IRideOfferer that posts a ride offer to the server
    /// using <see cref="RideService"/>.
    /// </summary>
    public class ServerRideOfferer : IRideOfferer
    {
        public async Task<IPendingRideRelatedRequest> SubmitRideOffer(RideOffer rideOffer)
        {
            string userId = App.Current.UserInfo.UserId;
            string requestId = await RideService.Instance.PostRideOfferAsync(userId, rideOffer);

            if (string.IsNullOrEmpty(requestId))
                return null;

            return new PollNotifications.PendingRideRelatedRequest(userId, requestId);
        }
    }
}
