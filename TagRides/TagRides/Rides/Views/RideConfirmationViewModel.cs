using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using Xamarin.Forms;
using TagRides.Utilities;
using TagRides.Rides;
using TagRides.Shared.Utilities;
using TagRides.Shared.RideData;
using TagRides.Shared.UserProfile;

namespace TagRides.Rides.Views
{
    public class RideConfirmationViewModel : INotifyPropertyChanged, IAnimatable
    {
        public ICommand ConfirmCommand => confirmCommand;
        public ICommand DeclineCommand => declineCommand;

        public double ExpireProgress
        {
            get => expireProgress;
            set
            {
                expireProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExpireProgress)));
            }
        }

        public RideInfo RideInfo { get; }
        public string DriverName => RideInfo.DriverId;
        public IEnumerable<UserInfo> Passengers => RideInfo.Route.Passengers;
        public bool HasPassengers => Passengers.Any();
        public bool NotHasPassengers => !Passengers.Any();

        /// <summary>
        /// The name of the first passenger. This exists only while the maximum
        /// number of passengers is 1.
        /// </summary>
        /// <value>The first name of the passenger.</value>
        public string FirstPassengerName
        {
            get
            {
                UserInfo passenger = Passengers.FirstOrDefault();

                if (passenger == null)
                    return null;

                return $"{passenger.NameFirst} {passenger.NameLast}";
            }
        }

        public RideConfirmationViewModel(
            Func<Task> confirmHandler,
            Func<Task> declineHandler,
            Func<Task> expireHandler,
            DateTime startTime,
            TimeSpan tillExpire,
            RideInfo rideInfo)
        {
            this.confirmHandler = confirmHandler;
            this.declineHandler = declineHandler;

            RideInfo = rideInfo;

            confirmCommand = new AsyncCommand(OnConfirm, () => !isBusy, App.Current.ErrorHandler);
            declineCommand = new AsyncCommand(OnDecline, () => !isBusy, App.Current.ErrorHandler);

            TimeSpan timeLeft = startTime.Add(tillExpire) - DateTime.Now;
            if (timeLeft.Milliseconds <= 0)
            {
                expireHandler().FireAndForgetAsync(App.Current.ErrorHandler);
                return;
            }

            //Make value slightly smaller than reality
            timeLeft = TimeSpan.FromMilliseconds(timeLeft.TotalMilliseconds * 0.9);

            Animation expireBarAnim = new Animation(
                (f) =>
                {
                    ExpireProgress = f;
                }, 
                1, 0);
            
            expireBarAnim.Commit(this, "expireProgress", length: (uint)timeLeft.TotalMilliseconds, finished: 
                (d, notFinished) =>
                {
                    if (!notFinished)
                        expireHandler().FireAndForgetAsync(App.Current.ErrorHandler);
                });
        }

        async Task OnConfirm()
        {
            isBusy = true;
            confirmCommand.ChangeCanExecute();

            await confirmHandler();
            this.AbortAnimation("expireProgress");

            isBusy = false;
            confirmCommand.ChangeCanExecute();
        }

        async Task OnDecline()
        {
            isBusy = true;
            declineCommand.ChangeCanExecute();

            await declineHandler();
            this.AbortAnimation("expireProgress");

            isBusy = false;
            declineCommand.ChangeCanExecute();
        }

        public void BatchBegin()
        {
        }

        public void BatchCommit()
        {
        }

        bool isBusy;
        readonly AsyncCommand confirmCommand;
        readonly AsyncCommand declineCommand;
        readonly Func<Task> confirmHandler;
        readonly Func<Task> declineHandler;
        double expireProgress = 1;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
