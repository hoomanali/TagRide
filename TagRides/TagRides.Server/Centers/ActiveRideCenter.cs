using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Server.Requests;
using TagRides.Server.Utility;

namespace TagRides.Server.Centers
{
    public static class ActiveRideCenter
    {
        #region Events

        public delegate void UserActionHandler(ActiveRideStatus status, string userId, bool isDriver);
        public delegate void RiderActionHandler(ActiveRideStatus status, string userId);
        

        public static event Action<ActiveRideStatus> RideStarted;
        public static event UserActionHandler UserCanceledRide;
        public static event RiderActionHandler UserJoinedRide;
        public static event UserActionHandler UserFinishedRide;
        public static event Action<ActiveRideStatus> RideEnded;

        #endregion

        public static async Task<string> ActiveRideStarted(
            RideInfo rideInfo, 
            UserRideOffer driver, 
            IEnumerable<UserRideRequest> riders)
        {
            ActiveRideStatus status =
                new ActiveRideStatus
                {
                    Id = IdGenerator.GenerateNewId(rideInfo),
                    Version = 0,
                    RideInfo = rideInfo,
                    RideState = ActiveRideStatus.State.InProgress,
                    RidersState = new ConcurrentDictionary<string, ActiveRideStatus.RiderState>(
                        riders.Select(
                            (r) => new KeyValuePair<string, ActiveRideStatus.RiderState>(
                                r.User.UserInfo.UserId,
                                ActiveRideStatus.RiderState.Waiting))),
                    RidersGameElements = new ConcurrentDictionary<string, RequestGameElements>(
                        riders
                            .Where((r) => r.RideRequest.GameElements != null)
                            .Select((r) => new KeyValuePair<string, RequestGameElements>(
                                r.User.UserInfo.UserId,
                                r.RideRequest.GameElements))),
                    DriverGameElements = driver.RideOffer.GameElements
                };

            Task postTask = Program.DataStore.PostActiveRideStatus(status);
            activeRides.TryAdd(status.Id, status);
            await postTask;

            RideStarted?.Invoke(status);

            return status.Id;
        }

        #region User methods

        /// <summary>
        /// Used to indicate that <paramref name="userId"/> has physically gotten into the car
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<bool> UserInRide(string rideId, string userId)
        {
            var statusAndIsRider = UserIsRider(rideId, userId);
            if (!statusAndIsRider.HasValue || !statusAndIsRider.Value.Item2)
                return false;

            var status = statusAndIsRider.Value.Item1;

            //Can't make changes if the ride is no longer active
            if (status.RideState != ActiveRideStatus.State.InProgress)
                return false;

            status.RidersState[userId] = ActiveRideStatus.RiderState.InRide;

            UserJoinedRide?.Invoke(status, userId);

            return await PostStatus(rideId);
        }

        /// <summary>
        /// The user has finished with the ride
        /// This is used for both riders and drivers, and has different implications for both
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<bool> UserFinished(string rideId, string userId)
        {
            var statusAndIsRider = UserIsRider(rideId, userId);
            if (!statusAndIsRider.HasValue)
                return false;

            var (status, isRider) = statusAndIsRider.Value;

            //Can't make changes if the ride is no longer active
            if (status.RideState != ActiveRideStatus.State.InProgress)
                return false;

            //Rider
            if (isRider)
            {
                status.RidersState[userId] = ActiveRideStatus.RiderState.DroppedOff;

                status.RideState = RiderBasedRideState(status);
            }
            //Driver
            else
            {
                //TODO this could happend even if not all riders are finished/canceled. Should that be allowed?
                status.RideState = ActiveRideStatus.State.Finished;

                RideEnded?.Invoke(status);
            }

            UserFinishedRide?.Invoke(status, userId, !isRider);

            return await PostStatus(rideId, true);
        }

        /// <summary>
        /// The user has canceled, and should no longer be apart of the ride.
        /// Used by both riders and drivers, and has different implications for both
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static async Task<bool> UserCanceled(string rideId, string userId)
        {
            var statusAndIsRider = UserIsRider(rideId, userId);
            if (!statusAndIsRider.HasValue)
                return false;

            var (status, isRider) = statusAndIsRider.Value;

            //Can't make changes if the ride is no longer active
            if (status.RideState != ActiveRideStatus.State.InProgress)
                return false;

            //Rider
            if (isRider)
            {
                //Cant cancel if you're not waiting
                if (status.RidersState[userId] != ActiveRideStatus.RiderState.Waiting)
                    return false;

                status.RidersState[userId] = ActiveRideStatus.RiderState.Canceled;

                status.RideState = RiderBasedRideState(status);

                if (status.RideState != ActiveRideStatus.State.Canceled && status.RideState != ActiveRideStatus.State.Finished)
                {
                    status.RideInfo =
                    new RideInfo(status.RideInfo.DriverId, status.RideInfo.Car,
                        new Route(
                            status.RideInfo.Route.Stops.Where(
                                (stop) => stop.Passenger != null &&
                                          stop.Passenger.UserId != userId).ToArray()
                    ));
                }
            }
            //Driver
            else
            {
                status.RideState = ActiveRideStatus.State.Canceled;
            }

            UserCanceledRide?.Invoke(status, userId, !isRider);
            return await PostStatus(rideId, true);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Is the user a rider
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="userId"></param>
        /// <returns>null if not a rider or driver</returns>
        static (ActiveRideStatus, bool)? UserIsRider(string rideId, string userId)
        {
            if (!activeRides.TryGetValue(rideId, out ActiveRideStatus status))
                return null;

            if (status.RideInfo.DriverId == userId)
                return (status, false);

            if (status.RidersState.ContainsKey(userId))
                return (status, true);

            return null;
        }

        /// <summary>
        /// Post the active ride to the dataStore
        /// </summary>
        /// <param name="rideId"></param>
        /// <param name="clearIfNotActive">if true, and the ride state is not InProgress, then the active ride is removed from this</param>
        /// <returns></returns>
        static async Task<bool> PostStatus(string rideId, bool clearIfNotActive = false)
        {
            if (!activeRides.TryGetValue(rideId, out ActiveRideStatus status))
                return false;

            status.Version++;

            await Program.DataStore.PostActiveRideStatus(status);

            if (clearIfNotActive && status.RideState != ActiveRideStatus.State.InProgress)
                activeRides.TryRemove(rideId, out status);

            return true;
        }

        /// <summary>
        /// Figures out what the RideState should be based only on the riders' states
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        static ActiveRideStatus.State RiderBasedRideState(ActiveRideStatus status)
        {
            if (status.RidersState.Values.Any((s) => s == ActiveRideStatus.RiderState.InRide || s == ActiveRideStatus.RiderState.Waiting))
                return ActiveRideStatus.State.InProgress;

            if (status.RidersState.Values.Any((s) => s == ActiveRideStatus.RiderState.DroppedOff))
                return ActiveRideStatus.State.Finished;

            return ActiveRideStatus.State.Canceled;
        }

        #endregion

        static readonly ConcurrentDictionary<string, ActiveRideStatus> activeRides = new ConcurrentDictionary<string, ActiveRideStatus>();
    }
}