using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Services.PollNotifications;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.DataStore;
using TagRides.Shared.Utilities;

namespace TagRides.Rides.States
{
    /// <summary>
    /// When in this state, the app is trying to determine whether the user
    /// has any pending requests or whether the user has been matched.
    /// </summary>
    public sealed class UnknownState : Ridesharing.StateBase
    {
        public UnknownState(Ridesharing ridesharing)
            : base(ridesharing)
        {
        }

        public UnknownState(Ridesharing.StateBase previous)
            : base(previous)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            Task.Run(DetermineState).OnError(ErrorHandler);
        }

        async Task DetermineState()
        {
            var nextState = await GetCurrentState();

            TransitionTo(nextState);
        }

        async Task<Ridesharing.StateBase> GetCurrentState()
        {
            string userId = App.Current.UserInfo.UserId;

            // Check if the user is currently in a ride.
            ActiveRideStatus activeRideStatus = await GetRideInProgress(userId);
            if (activeRideStatus != null)
                return new RideInProgressState(this, activeRideStatus);

            // Check if user has a ride request.
            RideRelatedRequestStatus requestStatus = await GetUnexpiredRequest(true, userId);
            if (requestStatus != null)
            {
                var result = await TryComputeRideRequestState(userId, requestStatus);

                if (result != null)
                    return result;
            }

            // Check if user has a ride offer.
            RideRelatedRequestStatus offerStatus = await GetUnexpiredRequest(false, userId);
            if (offerStatus != null)
            {
                var result = await TryComputeRideOfferState(userId, offerStatus);

                if (result != null)
                    return result;
            }

            return new NoneState(this);
        }

        async Task<RideRelatedRequestStatus> GetUnexpiredRequest(bool isRideRequest, string userId)
        {
            var pendingRequestIds = isRideRequest ?
                await DataService.Instance.GetPendingRideRequestIds(userId) :
                await DataService.Instance.GetPendingRideOfferIds(userId);

            foreach (string requestId in pendingRequestIds)
            {
                RideRelatedRequestStatus status = await App.Current
                    .DataStore.GetRideRelatedRequestStatus(userId, requestId);

                if (status != null && !status.IsExpired)
                    return status;
            }

            return null;
        }

        async Task<ActiveRideStatus> GetRideInProgress(string userId)
        {
            IEnumerable<string> activeRideIds = await DataService.Instance.GetActiveRideIds(userId);

            foreach (string activeRideId in activeRideIds)
            {
                ActiveRideStatus status = await App.Current.DataStore.GetActiveRideStatus(activeRideId);

                if (status.RideState == ActiveRideStatus.State.InProgress)
                    return status;
            }

            return null;
        }

        async Task<Ridesharing.StateBase> TryComputeRideRequestState(string userId, RideRelatedRequestStatus requestStatus)
        {
            string requestId = requestStatus.Id;
            string rideId = requestStatus.PendingRideId;

            if (string.IsNullOrEmpty(rideId))
            {
                var requestNotifications = new PendingRideRelatedRequest(userId, requestId);

                // FIXME Retrieve old RideRequest corresponding to requestId
                return new RequestPendingState(this, requestNotifications, null);
            }

            PendingRideStatus status = await App.Current.DataStore.GetPendingRideStatus(rideId);

            if (status != null)
            {
                switch (status.State)
                {
                    case PendingRideStatus.PendingRideState.Confirmed:
                        if (status.ActiveRideId == null)
                            break;

                        ActiveRideStatus activeRide = await App.Current.DataStore.GetActiveRideStatus(status.ActiveRideId);
                        if (activeRide == null)
                            break;

                        if (activeRide.RideState == ActiveRideStatus.State.InProgress)
                            return new RideInProgressState(this, activeRide);

                        break;

                    case PendingRideStatus.PendingRideState.WaitingOnRiders:
                        // TODO: Should return WaitingForConfirmedState if user already confirmed!
                        var matchNotifications = new MatchedRideRelatedRequest(userId, requestId, rideId);
                        return new RequestMatchedState(this, matchNotifications);

                    case PendingRideStatus.PendingRideState.WaitingOnDriver:
                    case PendingRideStatus.PendingRideState.Canceled:
                        break;
                }
            }

            return null;
        }

        async Task<Ridesharing.StateBase> TryComputeRideOfferState(string userId, RideRelatedRequestStatus offerStatus)
        {
            string requestId = offerStatus.Id;
            string rideId = offerStatus.PendingRideId;

            if (string.IsNullOrEmpty(rideId))
            {
                var requestNotifications = new PendingRideRelatedRequest(userId, requestId);

                // FIXME Retrieve old RideOffer corresponding to requestId
                return new OfferPendingState(this, requestNotifications, null);
            }

            PendingRideStatus status = await App.Current.DataStore.GetPendingRideStatus(rideId);

            if (status != null)
            {
                switch (status.State)
                {
                    case PendingRideStatus.PendingRideState.Confirmed:
                        if (status.ActiveRideId == null)
                            break;

                        ActiveRideStatus activeRide = await App.Current.DataStore.GetActiveRideStatus(status.ActiveRideId);
                        if (activeRide == null)
                            break;

                        if (activeRide.RideState == ActiveRideStatus.State.InProgress)
                            return new RideInProgressState(this, activeRide);

                        break;

                    case PendingRideStatus.PendingRideState.WaitingOnDriver:
                        return new OfferMatchedState(this, new MatchedRideRelatedRequest(userId, requestId, rideId));

                    case PendingRideStatus.PendingRideState.WaitingOnRiders:
                        return new WaitingForConfirmedState(this, new MatchedRideRelatedRequest(userId, requestId, rideId));

                    case PendingRideStatus.PendingRideState.Canceled:
                        break;
                }
            }

            return null;
        }
    }
}
