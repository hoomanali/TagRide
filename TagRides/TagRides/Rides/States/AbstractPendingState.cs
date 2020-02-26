using System;
using System.Threading.Tasks;
using TagRides.Services;
namespace TagRides.Rides.States
{
    /// <summary>
    /// Used as the common base class for the offer pending state and
    /// request pending state.
    /// </summary>
    public abstract class AbstractPendingState : Ridesharing.StateBase
    {
        protected readonly IPendingRideRelatedRequest response;

        protected AbstractPendingState(Ridesharing.StateBase previous, IPendingRideRelatedRequest response)
            : base(previous)
        {
            this.response = response;
        }

        protected override void Initialize()
        {
            base.Initialize();

            response.OnMatched += HandleMatched;
            response.OnCanceled += HandleCanceled;
            response.OnExpired += HandleExpired;
        }

        public async Task<bool> Cancel()
        {
            if (IsStale)
                return false;

            bool success = await response.Cancel();

            if (!success)
                return false;

            TransitionTo(new NoneState(this));

            return true;
        }

        protected new void TransitionTo(Ridesharing.StateBase next)
        {
            UnregisterListeners();
            base.TransitionTo(next);
        }

        void UnregisterListeners()
        {
            // TODO This may happen multiple times due to multithreading.
            // Hopefully that is okay.
            response.OnMatched -= HandleMatched;
            response.OnCanceled -= HandleCanceled;
            response.OnExpired -= HandleExpired;
        }

        protected abstract void HandleMatched(IMatchedRideRelatedRequest match);

        void HandleCanceled()
        {
            TransitionTo(new NoneState(this));
        }

        void HandleExpired()
        {
            TransitionTo(new NoneState(this));
        }
    }
}
