using System;
using System.Threading.Tasks;
using TagRides.Shared.RideData.Status;
using TagRides.Services;
using TagRides.Shared.Utilities;

namespace TagRides.Rides.States
{
    public class RideInProgressState : Ridesharing.StateBase
    {
        public ActiveRideStatus MostRecentRideStatus { get; }

        public RideInProgressState(Ridesharing.StateBase previous, ActiveRideStatus rideStatus)
            : base(previous)
        {
            MostRecentRideStatus = rideStatus;
        }

        public async Task<bool> Finish()
        {
            if (IsStale)
                return false;

            // FIXME Race condition (what if Finish() invoked multiple times concurrently)
            await RideService.Instance.PostActiveRideFinishAsync(
                App.Current.UserInfo.UserId, MostRecentRideStatus.Id);

            TransitionTo(new NoneState(this));

            return true;
        }

        public async Task<bool> Cancel()
        {
            if (IsStale)
                return false;

            // FIXME Race condition (what if Cancel() invoked multiple times concurrently)
            await RideService.Instance.PostActiveRideCancelAsync(
                App.Current.UserInfo.UserId, MostRecentRideStatus.Id);

            TransitionTo(new NoneState(this));

            return true;
        }
    }
}
