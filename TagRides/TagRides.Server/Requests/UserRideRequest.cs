using System;
using System.Collections.Generic;
using TagRides.Server.UserData;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Server.Rides;
using TagRides.Shared.Utilities;
using System.Threading.Tasks;

namespace TagRides.Server.Requests
{
    public class UserRideRequest : UserRideRelatedRequest
    {
        public enum RideRequestState
        {
            /// <summary>
            /// The ride request is waiting to be matched
            /// </summary>
            Unmatched,
            /// <summary>
            /// Part of a pending ride, but waiting for the driver to confirm.
            /// </summary>
            WaitingForDriverConfirmation,
            /// <summary>
            /// The request has been matched, and is waiting on the requesting user to confirm
            /// </summary>
            Unconfirmed,
            /// <summary>
            /// The user and driver have confirmed, thus completing the life cycle of the request.
            /// (Not all the users of the request are guaranteed to have confirmed)
            /// </summary>
            Matched,
            /// <summary>
            /// The request has expired, and no more action to match it will be taken
            /// </summary>
            Expired
        }

        public readonly RideRequest RideRequest;

        public UserRideRequest(User user, RideRequest rideRequest)
            : base(user)
        {
            RideRequest = rideRequest;

            PostStatus().FireAndForgetAsync(Program.ErrorHandler);
        }

        public RideRequestState State => state;

        #region Status update methods

        public override void PendingRideMade(PendingRide pendingRide)
        {
            if (state != RideRequestState.Unmatched)
                throw new Exception("Added to pending ride in a status other than Unmatched");

            state = RideRequestState.WaitingForDriverConfirmation;
            pendingRideId = pendingRide.Id;

            //No update to data store until driver has confirmed

            InvokeApartOfPendingRide(pendingRide);
            InvokeStateUpdated();
        }

        public void DriverConfirmed()
        {
            if (state != RideRequestState.WaitingForDriverConfirmation)
                throw new Exception("DriverConfirmed in a status other than WaitingForDriverConfirmation");

            state = RideRequestState.Unconfirmed;

            PostStatus().FireAndForgetAsync(Program.ErrorHandler);

            InvokeStateUpdated();
        }

        public override void UserConfirmed()
        {
            if (state != RideRequestState.Unconfirmed)
                throw new Exception("UserConfirmed in a status other than Unconfirmed");

            state = RideRequestState.Matched;

            InvokeStateUpdated();
        }

        public override void SetExpired()
        {
            state = RideRequestState.Expired;

            PostStatus().FireAndForgetAsync(Program.ErrorHandler);

            InvokeStateUpdated();
        }
        
        public void Unmatched()
        {
            state = RideRequestState.Unmatched;
            pendingRideId = null;

            InvokeStateUpdated();
        }

        #endregion

        async Task PostStatus()
        {
            await Program.DataStore.PostRideRelatedRequestStatus(User.UserInfo.UserId, Id,
                new RideRelatedRequestStatus
                {
                    Id = Id,
                    IsExpired = state == RideRequestState.Expired,
                    PendingRideId = pendingRideId,
                    Version = statusVersion++
                });
        }

        RideRequestState state = RideRequestState.Unmatched;
        string pendingRideId = null;
        int statusVersion = 0;
    }
}