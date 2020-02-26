using System;
using System.Threading.Tasks;
using TagRides.Shared.RideData.Status;
using TagRides.Services;
using TagRides.Rides;

namespace TagRides.Services.PollNotifications
{
    public class MatchedRideRelatedRequest : IMatchedRideRelatedRequest
    {
        public PendingRideStatus MostRecentStatus => mostRecentStatus;

        public event Action<PendingRideStatus> OnStatusUpdated
        {
            add => statusTracker.StartTracking(value);
            remove => statusTracker.RemoveListener(value);
        }

        public MatchedRideRelatedRequest(string userId, string requestId, string pendingRideId)
        {
            this.userId = userId;
            this.requestId = requestId;
            this.pendingRideId = pendingRideId;

            var getter = new PendingRideStatusGetter(pendingRideId);

            mostRecentStatus = getter.GetStatusAsync().Result;

            statusTracker = new StatusTracker<PendingRideStatus>(
                getter,
                App.Current.ErrorHandler);

            // FIXME: StatusTracker in MatchedRideRelatedRequest might track longer than necessary.
            statusTracker.StartTracking(updated => mostRecentStatus = updated);
        }

        public Task<bool> Confirm()
        {
            return RideService.Instance.PostConfirmAsync(userId, pendingRideId);
        }

        public Task<bool> Decline()
        {
            return RideService.Instance.PostRequestCancelAsync(userId, requestId);
        }

        PendingRideStatus mostRecentStatus;

        readonly string userId;
        readonly string requestId;
        readonly string pendingRideId;
        readonly StatusTracker<PendingRideStatus> statusTracker;
    }
}
