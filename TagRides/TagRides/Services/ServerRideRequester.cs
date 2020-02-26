using System.Threading.Tasks;
using TagRides.Rides;
using TagRides.Shared.RideData;

namespace TagRides.Services
{
    public class ServerRideRequester : IRideRequester
    {
        public async Task<IPendingRideRelatedRequest> SubmitRideRequest(RideRequest rideRequest)
        {
            string userId = App.Current.UserInfo.UserId;
            string requestId = await RideService.Instance.PostRideRequestAsync(userId, rideRequest);

            if (string.IsNullOrEmpty(requestId))
                return null;

            return new PollNotifications.PendingRideRelatedRequest(userId, requestId);
        }
    }
}
