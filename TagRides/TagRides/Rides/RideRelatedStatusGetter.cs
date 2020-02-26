using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Shared.RideData.Status;

namespace TagRides.Rides
{
    public class RideRelatedStatusGetter : IStatusGetter<RideRelatedRequestStatus>
    {
        // The user ID for which a ride offer was submitted.
        readonly string userId;
        readonly string requestId;

        public RideRelatedStatusGetter(string userId, string requestId)
        {
            this.userId = userId;
            this.requestId = requestId;
        }

        /// <summary>
        /// Gets the status of the submitted ride offer. This may return null,
        /// which means that no status information is available.
        /// </summary>
        /// <returns>The status of the submitted ride offer.</returns>
        public async Task<RideRelatedRequestStatus> GetStatusAsync()
        {
            return await RideService.Instance.PollRideRelatedRequestAsync(userId, requestId);
        }
    }
}
