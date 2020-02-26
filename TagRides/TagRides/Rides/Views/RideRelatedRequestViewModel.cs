using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TagRides.Shared.Game;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;
using TagRides.Utilities;

namespace TagRides.Rides.Views
{
    public abstract class RideRelatedRequestViewModel : INotifyPropertyChanged
    {
        public Func<IDisposable> DisplayActivityIndicator { get; set; }

        public RideRelatedRequestViewModel(
            Func<RequestBase, Task> submit,
            Func<Task> cancel,
            Action<GeoCoordinates, Action<NamedLocation>> pickLocation,
            NamedLocation start,
            NamedLocation end)
        {
            this.start = start;
            this.end = end;
            departureTime = DateTime.Now;

            SubmitRequestCommand = new AsyncCommand(SubmitWrapper, null, App.Current.ErrorHandler);
            CancelRequestCommand = new AsyncCommand(CancelWrapper, null, App.Current.ErrorHandler);

            this.pickLocation = pickLocation;

            async Task SubmitWrapper()
            {
                using (var token = DisplayActivityIndicator?.Invoke())
                    await submit(MakeRequest());
            }

            async Task CancelWrapper()
            {
                using (var token = DisplayActivityIndicator?.Invoke())
                    await cancel();
            }
        }

        public ICommand SubmitRequestCommand { get; private set; }
        public ICommand CancelRequestCommand { get; private set; }

        public string Start => start.Name;
        public string End => end.Name;

        public TimeSpan DepartureTime
        {
            get => departureTime.TimeOfDay;
            set
            {
                departureTime = departureTime.Date.Add(value);
                OnPropertyChanged(nameof(DepartureTime));
            }
        }

        public DateTime DepartureDate
        {
            get => departureTime.Date;
            set
            {
                departureTime = value.Add(departureTime.TimeOfDay);
                OnPropertyChanged(nameof(DepartureDate));
            }
        }

        public GameItem EquippedItem
        {
            get => equippedItem;
            set
            {
                equippedItem = value;
                OnPropertyChanged(nameof(EquippedItem));
            }
        }

        public void ChangeStart()
        {
            pickLocation?.Invoke(start.Coordinates,
                (namedLoc) =>
                {
                    start = namedLoc;
                    OnPropertyChanged(nameof(Start));
                });
        }

        public void ChangeEnd()
        {
            pickLocation?.Invoke(end.Coordinates,
                (namedLoc) =>
                {
                    end = namedLoc;
                    OnPropertyChanged(nameof(End));
                });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected abstract RequestBase MakeRequest();
        
        protected NamedLocation start;
        protected NamedLocation end;
        protected DateTime departureTime;
        protected GameItem equippedItem = null;

        readonly Action<GeoCoordinates, Action<NamedLocation>> pickLocation;
    }
}
