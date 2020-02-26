using System;
using System.Threading;
using System.Threading.Tasks;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData.Status;
using TagRides.Services;

namespace TagRides.Rides
{
    public class StatusTracker<T> where T : Status
    {
        public int PollTime { get; set; } = 2000;

        public bool IsTracking { get; private set; }

        public StatusTracker(IStatusGetter<T> statusGetter, IErrorHandler errorHandler = null)
        {
            this.errorHandler = errorHandler;
            this.statusGetter = statusGetter;
            IsTracking = false;
        }

        public void StartTracking(Action<T> handleStatusUpdated)
        {
            OnStatusUpdated += handleStatusUpdated;

            if (cancellationTokenSource != null)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Task.Run(
                async () => await PollForStatusAsync(cancellationToken),
                cancellationToken).OnError(errorHandler);

            IsTracking = true;
        }

        public void StopTracking()
        {
            IsTracking = false;

            // FIXME: There may be threading issues in StopTracking() because the poll method calls it.
            if (cancellationTokenSource == null)
                return;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            // StartTracking() checks if this is null.
            cancellationTokenSource = null;

            OnStatusUpdated = null;
        }

        public void RemoveListener(Action<T> listener)
        {
            OnStatusUpdated -= listener;

            // If there are no more listeners left, stop tracking.
            if (OnStatusUpdated == null)
                StopTracking();
        }

        /// <summary>
        /// Polls the ride offer status until the status is matched or becomes null.
        /// </summary>
        /// <returns>The for status async.</returns>
        async Task PollForStatusAsync(CancellationToken cancellationToken)
        {
            T status = null;

            do
            {
                try
                {
                    await Task.Delay(PollTime, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                }

                cancellationToken.ThrowIfCancellationRequested();

                T newStatus = await statusGetter.GetStatusAsync();

                if (status == null && newStatus == null)
                    throw new Exception("No status to poll");

                if ((status == null) != (newStatus == null) || status.Version != newStatus.Version)
                {
                    status = newStatus;
                    OnStatusUpdated?.Invoke(status);
                }

            } while (status != null);

            StopTracking();
        }

        CancellationTokenSource cancellationTokenSource;

        readonly IStatusGetter<T> statusGetter;
        readonly IErrorHandler errorHandler;

        /// <summary>
        /// Private event that occurs when the offer status updates in PollForStatusAsync().
        /// This is private so that the only way to register to this event is to call
        /// <see cref="StartTracking"/> and so that
        /// <see cref="StopTracking"/> can safely remove all listeners without
        /// causing unexpected behavior.
        /// </summary>
        event Action<T> OnStatusUpdated;
    }
}
