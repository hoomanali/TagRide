using System;
using System.Threading.Tasks;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;

namespace TagRides.Services
{
    public interface IRideRequester
    {
        Task<IPendingRideRelatedRequest> SubmitRideRequest(RideRequest rideRequest);
    }

    public interface IRideOfferer
    {
        Task<IPendingRideRelatedRequest> SubmitRideOffer(RideOffer rideOffer);
    }

    public interface IPendingRideRelatedRequest
    {
        event Action<IMatchedRideRelatedRequest> OnMatched;
        event Action OnExpired;
        event Action OnCanceled;

        Task<bool> Cancel();
    }

    public interface IMatchedRideRelatedRequest
    {
        event Action<PendingRideStatus> OnStatusUpdated;

        PendingRideStatus MostRecentStatus { get; }

        Task<bool> Confirm();
        Task<bool> Decline();
    }

    public interface IStatusGetter<T> where T : Status
    {
        Task<T> GetStatusAsync();
    }
}
