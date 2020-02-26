using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Shared.RideData.Status;

namespace TagRides.Rides
{
    public class PendingRideStatusGetter : IStatusGetter<PendingRideStatus>
    {
        readonly string pendingRideId;

        public PendingRideStatusGetter(string pendingRideId)
        {
            this.pendingRideId = pendingRideId;
        }

        public async Task<PendingRideStatus> GetStatusAsync()
        {
            return await RideService.Instance.PollPendingRideStatusAsync(pendingRideId);
        }
    }
}
