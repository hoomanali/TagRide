using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Shared.RideData.Status;
namespace TagRides.Rides.States
{
    public abstract class AbstractMatchedState : Ridesharing.StateBase
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

        protected readonly IMatchedRideRelatedRequest match;

        protected AbstractMatchedState(Ridesharing.StateBase previous, IMatchedRideRelatedRequest match)
            : base(previous)
        {
            this.match = match;
        }

        public async Task<bool> Confirm()
        {
            if (IsStale)
                return false;

            if (await match.Confirm())
            {
                TransitionTo(new WaitingForConfirmedState(this, match));
                return true;
            }

            return false;
        }

        public async Task<bool> Decline()
        {
            if (IsStale)
                return false;

            if (await match.Decline())
            {
                // TODO: When the user declines a match, should they go back to the None state?
                TransitionTo(new NoneState(this));
                return true;
            }

            return false;
        }

        protected new void TransitionTo(Ridesharing.StateBase next)
        {
            foreach (var listener in statusListeners)
                OnStatusUpdated -= listener;
            statusListeners.Clear();

            base.TransitionTo(next);
        }

        readonly HashSet<Action<PendingRideStatus>> statusListeners = new HashSet<Action<PendingRideStatus>>();
    }
}
