using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace TagRides.Shared.RideData.Status
{
    [JsonObject]
    public class Status
    {
        public int Version;
        public string Id;
    }

    /// <summary>
    /// Status used by ride requests and offers
    /// </summary>
    [JsonObject]
    public class RideRelatedRequestStatus : Status
    {
        public string PendingRideId;
        public bool IsExpired;
    }

    [JsonObject]
    public class PendingRideStatus : Status
    {
        //TODO currently this is identical to the enum in PendingRide
        //No need to have this in both places, probably...
        public enum PendingRideState
        {
            WaitingOnDriver,
            WaitingOnRiders,
            Confirmed,
            Canceled
        }

        public PendingRideState State;
        public RideInfo RideInfo;
        /// <summary>
        /// The time this status was posted
        /// </summary>
        public DateTime PostTime;
        /// <summary>
        /// The time from PostTime until the request will be considered expired
        /// </summary>
        public TimeSpan TimeTillExpire;
        public string ActiveRideId;
    }

    [JsonObject]
    public class ActiveRideStatus : Status
    {
        public enum RiderState
        {
            Waiting,
            InRide,
            DroppedOff,
            Canceled
        }

        public enum State
        {
            InProgress,
            Finished,
            Canceled
        }

        public RideInfo RideInfo;

        public State RideState;

        public ConcurrentDictionary<string, RiderState> RidersState;

        //TODO should we serialize this?
        public ConcurrentDictionary<string, RequestGameElements> RidersGameElements;

        public RequestGameElements DriverGameElements;
    }
}
