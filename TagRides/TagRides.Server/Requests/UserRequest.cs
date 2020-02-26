using System;
using System.Collections.Generic;
using System.Threading;
using TagRides.Server.Rides;
using TagRides.Server.UserData;
using TagRides.Shared.Utilities;

namespace TagRides.Server.Requests
{
    public class UserRequest
    {
        #region Events

        /// <summary>
        /// Details of the request are changed (ex. start location, time, ect)
        /// </summary>
        public event Action<UserRequest> Changed;
        protected void InvokeChanged() => Changed?.Invoke(this);

        /// <summary>
        /// The state of the request changes (ex. You got a match)
        /// </summary>
        public event Action<UserRequest> StateUpdated;
        protected void InvokeStateUpdated() => StateUpdated?.Invoke(this);

        /// <summary>
        /// Occurs when the request is canceled. Can only occur once.
        /// </summary>
        public IThreadSafeOneTimeEvent<UserRequest> Canceled;

        #endregion

        public void Cancel()
        {
            canceledToken.RaiseEvent(this);
        }

        public readonly User User;
        public readonly string Id;

        public UserRequest(User user)
        {
            User = user;
            Id = Utility.IdGenerator.GenerateNewId(this);

            canceledToken = new RaisableEvent<UserRequest>();
            Canceled = new ThreadSafeOneTimeEvent<UserRequest>(canceledToken);
        }

        readonly RaisableEvent<UserRequest> canceledToken;
    }

    public abstract class UserRideRelatedRequest : UserRequest
    {
        public UserRideRelatedRequest(User user)
            : base(user) { }

        public event Action<UserRideRelatedRequest, PendingRide> ApartOfPendingRide;
        protected void InvokeApartOfPendingRide(PendingRide pendingRide) => ApartOfPendingRide?.Invoke(this, pendingRide);

        public abstract void PendingRideMade(PendingRide pendingRide);
        public abstract void UserConfirmed();
        public abstract void SetExpired();
    }
}