using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TagRides.Shared.Geo;
using TagRides.Shared.RideData;
using TagRides.Shared.UserProfile;
using TagRides.Shared.Game;
using TagRides.Utilities;
using Xamarin.Forms;

namespace TagRides.Rides.Views
{
    public class RideOfferViewModel : RideRelatedRequestViewModel
    {
        public RideOfferViewModel(
            Func<RequestBase, Task> submit, 
            Func<Task> cancel,
            Action<GeoCoordinates, Action<NamedLocation>> pickLocation,
            NamedLocation start,
            NamedLocation end)
            : base(submit, cancel, pickLocation, start, end)
        {}

        public double MaxTimeOutOfWay
        {
            get => maxTimeOutOfWay;
            set
            {
                maxTimeOutOfWay = value;
                OnPropertyChanged(nameof(MaxTimeOutOfWay));
            }
        }

        public CarInfo Car
        {
            get => car;
            set
            {
                car = value;
                OnPropertyChanged(nameof(Car));

                if (car != null) AvailableSeats = car.DefaultCapacity;
            }
        }

        public int AvailableSeats
        {
            get => availableSeats;
            set
            {
                availableSeats = value;
                OnPropertyChanged(nameof(AvailableSeats));
            }
        }

        protected override RequestBase MakeRequest()
        {
            GameItem item = equippedItem.EffectType == GameItem.Effect.None ? null : equippedItem;

            return new RideOffer(
                new Trip(departureTime, start.Coordinates, end.Coordinates),
                maxTimeOutOfWay,
                car,
                availableSeats,
                new RequestGameElements(item));
        }
        
        double maxTimeOutOfWay = 1;
        CarInfo car;
        int availableSeats;
    }
}
