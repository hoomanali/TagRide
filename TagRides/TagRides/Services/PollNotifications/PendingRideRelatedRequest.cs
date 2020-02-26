using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using TagRides.Rides;
using TagRides.Shared.RideData.Status;

namespace TagRides.Services.PollNotifications
{
    public class PendingRideRelatedRequest : IPendingRideRelatedRequest
    {
        public event Action<IMatchedRideRelatedRequest> OnMatched
        {
            add
            {
                AddStatusListener(value, status =>
                {
                    if (!string.IsNullOrEmpty(status.PendingRideId))
                        value(new MatchedRideRelatedRequest(userId, requestId, status.PendingRideId));
                });
            }

            remove
            {
                RemoveStatusListener(value);
            }
        }

        public event Action OnExpired
        {
            add
            {
                AddStatusListener(value, status =>
                {
                    if (status.IsExpired)
                        value();
                });
            }

            remove
            {
                RemoveStatusListener(value);
            }
        }

        public event Action OnCanceled
        {
            // TODO: Implement OnCanceled event for pending requests.
            add => OnExpired += value;
            remove => OnExpired -= value;
        }

        public PendingRideRelatedRequest(string userId, string requestId)
        {
            this.userId = userId;
            this.requestId = requestId;

            var statusGetter = new RideRelatedStatusGetter(userId, requestId);
            statusTracker = new StatusTracker<RideRelatedRequestStatus>(statusGetter, App.Current.ErrorHandler);
        }

        public Task<bool> Cancel()
        {
            return RideService.Instance.PostRequestCancelAsync(userId, requestId);
        }

        void AddStatusListener(object key, Action<RideRelatedRequestStatus> listener)
        {
            if (!listenerMap.ContainsKey(key))
            {
                OnStatusUpdated += listener;
                listenerMap[key] = listener;
            }
        }

        void RemoveStatusListener(object key)
        {
            if (listenerMap.TryGetValue(key, out var listener))
                OnStatusUpdated -= listener;
        }

        event Action<RideRelatedRequestStatus> OnStatusUpdated
        {
            add => statusTracker.StartTracking(value);
            remove => statusTracker.RemoveListener(value);
        }

        readonly string userId;
        readonly string requestId;
        readonly StatusTracker<RideRelatedRequestStatus> statusTracker;

        readonly Dictionary<object, Action<RideRelatedRequestStatus>> listenerMap
            = new Dictionary<object, Action<RideRelatedRequestStatus>>();
    }
}
