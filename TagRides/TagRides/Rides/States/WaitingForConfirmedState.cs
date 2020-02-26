using System;
using System.Collections.Generic;
using TagRides.Services;
using TagRides.Shared.RideData.Status;

namespace TagRides.Rides.States
{
    public class WaitingForConfirmedState : Ridesharing.StateBase
    {
        public PendingRideStatus MostRecentStatus => match.MostRecentStatus;
        public event Action<PendingRideStatus> OnStatusUpdated
        {
            add
            {
                if (statusListeners.Add(value))
                    match.OnStatusUpdated += value;
            }

            remove
            {
                if (statusListeners.Remove(value))
                    match.OnStatusUpdated -= value;
            }
        }

        public WaitingForConfirmedState(Ridesharing.StateBase previous, IMatchedRideRelatedRequest match)
            : base(previous)
        {
            this.match = match;
        }

        protected override void Initialize()
        {
            base.Initialize();

            match.OnStatusUpdated += Handler;
            Handler(match.MostRecentStatus);

            void Handler(PendingRideStatus status)
            {
                switch (status.State)
                {
                    case PendingRideStatus.PendingRideState.Confirmed:
                        TransitionTo(new RideInProgressState(this, App.Current.DataStore.GetActiveRideStatus(status.ActiveRideId).Result));
                        match.OnStatusUpdated -= Handler;
                        break;

                    case PendingRideStatus.PendingRideState.Canceled:
                        // TODO Inform user when ride was canceled.
                        TransitionTo(new NoneState(this));
                        match.OnStatusUpdated -= Handler;
                        break;
                }
            }
        }

        protected new void TransitionTo(Ridesharing.StateBase next)
        {
            foreach (var listener in statusListeners)
                OnStatusUpdated -= listener;

            base.TransitionTo(next);
        }

        // TODO: WaitingForConfirmedState should not use IMatchedRideRelatedRequest because it cannot Confirm or Decline
        readonly IMatchedRideRelatedRequest match;
        readonly HashSet<Action<PendingRideStatus>> statusListeners = new HashSet<Action<PendingRideStatus>>();
    }
}
