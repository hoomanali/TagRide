using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    /// <summary>
    /// A wrapper view that can be used to display an activity indicator overlay.
    /// </summary>
    [ContentProperty(nameof(NormalContent))]
    public partial class AsyncActivityView : ContentView
    {
        // NOTE: The reason this isn't declared as 'public new View Content' is
        // because the XAML compiler will still use ContentView's Content property!
        /// <summary>
        /// The normal content to display.
        /// </summary>
        public View NormalContent
        {
            get => InnerLayout.Children[0];
            set => InnerLayout.Children[0] = value;
        }

        /// <summary>
        /// Is the view currently displaying an activity indicator?
        /// </summary>
        public bool IsWaiting => activityCount > 0;

        /// <summary>
        /// Displays an activity indicator over the entire view. Use in a
        /// <see langword="using"/> statement, or call Dispose() manually when
        /// the activity is finished. It is okay to call BeginActivity() multiple
        /// times --- the activity indicator will be displayed until all IDisposables
        /// have been disposed.
        /// </summary>
        /// <returns>An IDisposable where Dispose() marks the activity as done.</returns>
        public IDisposable BeginActivity()
        {
            return new ActivityToken(this);
        }

        public AsyncActivityView()
        {
            InitializeComponent();
            UpdateActivityIndicator(false);
        }

        void ChangeActivityCount(int change)
        {
            // Lock so that this works with multithreading. This is important if
            // the ActivityToken is disposed from a different thread.
            lock (waitingLock)
            {
                activityCount += change;

                bool isWaiting = IsWaiting;

                // ASSUMPTION: These run in the order they are invoked.
                Device.BeginInvokeOnMainThread(() => UpdateActivityIndicator(isWaiting));
            }
        }

        void UpdateActivityIndicator(bool isWaiting)
        {
            WaitingIndicator.IsVisible = isWaiting;
            WaitingIndicator.IsRunning = isWaiting;
        }

        int activityCount;
        readonly object waitingLock = new object();

        class ActivityToken : IDisposable
        {
            public ActivityToken(AsyncActivityView v)
            {
                view = v;
                view.ChangeActivityCount(1);
            }

            void IDisposable.Dispose()
            {
                // Ensure we only dispose once, even with concurrency.
                // Exchange() returns the original value.
                if (Interlocked.Exchange(ref numDisposed, 1) > 0)
                    return;

                view.ChangeActivityCount(-1);
            }

            int numDisposed;
            readonly AsyncActivityView view;
        }
    }
}
