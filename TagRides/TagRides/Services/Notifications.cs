using System;
using TagRides.Shared.RideData.Status;

namespace TagRides.Services
{
    public interface IRideOfferNotificationSource
    {
        event Action<RideRelatedRequestStatus> OnOfferStatusUpdated;
    }

    public interface IRideRequestNotificationSource
    {
        event Action<RideRelatedRequestStatus> OnRequestStatusUpdated;
    }
}
