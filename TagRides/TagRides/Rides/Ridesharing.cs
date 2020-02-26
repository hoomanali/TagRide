using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TagRides.Services;
using TagRides.Shared.RideData;
using TagRides.Shared.Utilities;
using TagRides.Utilities;
using System.Diagnostics;

namespace TagRides.Rides
{
    /// <summary>
    /// Contains ridesharing business logic.
    /// </summary>
    public class Ridesharing
    {
        public event Action<StateBase> OnRidesharingStateChanged;

        public StateBase RidesharingState { get; private set; }

        public IRideRequester RideRequester { get; set; }
        public IRideOfferer RideOfferer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:TagRides.Rides.Rides"/> class.
        /// </summary>
        /// <param name="errorHandler">The error handler to invoke when a command
        /// fails.</param>
        public Ridesharing(IErrorHandler errorHandler)
        {
            this.errorHandler = errorHandler;

            StateBase.Initialize(this);
        }

        readonly IErrorHandler errorHandler;


        public abstract class StateBase
        {
            public static void Initialize(Ridesharing ridesharing)
            {
                ridesharing.RidesharingState = new States.UnknownState(ridesharing);
                ridesharing.RidesharingState.Initialize();
            }

            /// <summary>
            /// Has this state been transitioned out of?
            /// </summary>
            protected bool IsStale => transitionCount > 0;

            protected StateBase(Ridesharing ridesharing)
            {
                this.ridesharing = ridesharing;
                transitionCount = 0;
            }

            protected StateBase(StateBase previousState)
            {
                if (previousState.IsStale)
                    throw new ApplicationException("Cannot transition from a stale state.");

                ridesharing = previousState.ridesharing;
                transitionCount = 0;
            }

            protected virtual void Initialize() { }

            protected void TransitionTo(StateBase otherState)
            {
                // Atomically updates transitionCount to 1 if it was at 0.
                int oldCount = Interlocked.CompareExchange(ref transitionCount, 1, 0);

                // This can happen either due to incorrect code or due to race
                // conditions. To simplify the code in derived classes, this
                // just logs a message to the console (for debugging) and otherwise
                // silently does nothing.
                if (oldCount == 1)
                {
                    Console.WriteLine("Tried to transition from a stale state.");
                    return;
                }

                Debug.Assert(otherState.ridesharing == ridesharing);
                ridesharing.RidesharingState = otherState;
                ridesharing.OnRidesharingStateChanged?.Invoke(otherState);
                otherState.Initialize();
            }

            protected IRideOfferer RideOfferer => ridesharing.RideOfferer;
            protected IRideRequester RideRequester => ridesharing.RideRequester;
            protected IErrorHandler ErrorHandler => ridesharing.errorHandler;

            int transitionCount;
            readonly Ridesharing ridesharing;
        }

    }
}
