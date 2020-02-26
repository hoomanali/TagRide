using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagRides.Server.Rides;
using TagRides.Server.UserData;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.Utilities;

namespace TagRides.Server.Requests
{
    public class UserRideOffer : UserRideRelatedRequest
    {
        public enum RideOfferState
        {
            /// <summary>
            /// Waititng to be matched
            /// </summary>
            Unmatched,
            /// <summary>
            /// In a pending ride, waiting for user to confirm
            /// </summary>
            Unconfirmed,
            /// <summary>
            /// In a pending ride, and has confirmed. Concludes the life cycle of this
            /// </summary>
            Matched,
            /// <summary>
            /// Was in a pending ride that is no longer active
            /// </summary>
            Expired
        }

        public readonly RideOffer RideOffer;

        public UserRideOffer(User user, RideOffer rideOffer)
            : base(user)
        {
            RideOffer = rideOffer;

            PostStatus(null).FireAndForgetAsync(Program.ErrorHandler);
        }

        public RideOfferState State => state;

        public override void PendingRideMade(PendingRide pendingRide)
        {
            if (state != RideOfferState.Unmatched)
                throw new Exception("Added to pending ride in a status other than Unmatched");

            state = RideOfferState.Unconfirmed;

            PostStatus(pendingRide.Id).FireAndForgetAsync(Program.ErrorHandler);

            InvokeApartOfPendingRide(pendingRide);
            InvokeStateUpdated();
        }

        public override void UserConfirmed()
        {
            if (state != RideOfferState.Unconfirmed)
                throw new Exception("UserConfirmed in a status other than UNconfirmed");

            state = RideOfferState.Matched;

            InvokeStateUpdated();
        }

        public override void SetExpired()
        {
            state = RideOfferState.Expired;

            PostStatus(null).FireAndForgetAsync(Program.ErrorHandler);

            InvokeStateUpdated();
        }
        
        async Task PostStatus(string pendingRideId)
        {
            await Program.DataStore.PostRideRelatedRequestStatus(User.UserInfo.UserId, Id,
                new RideRelatedRequestStatus
                {
                    Id = Id,
                    IsExpired = state == RideOfferState.Expired,
                    PendingRideId = pendingRideId,
                    Version = statusVersion++
                });
        }

        RideOfferState state = RideOfferState.Unmatched;
        int statusVersion = 0;
    }
}