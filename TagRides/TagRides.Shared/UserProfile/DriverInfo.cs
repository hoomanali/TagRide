using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace TagRides.Shared.UserProfile
{
    /// <summary>
    /// Driver specific information
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class DriverInfo : INotifyPropertyChanged
    {
        [JsonProperty]
        ObservableCollection<CarInfo> cars = new ObservableCollection<CarInfo>();
        [JsonProperty(IsReference = true)]
        CarInfo defaultCar;

        public DriverInfo()
        {
            cars.CollectionChanged += OnCarsModified;
        }

        public CarInfo DefaultCar
        {
            get => defaultCar;

            set
            {
                if (value == defaultCar) return;
                if (value != null && !cars.Contains(value)) return;

                defaultCar = value;

                OnPropertyChanged("DefaultCar");

                if (defaultCar != null) defaultCar.IsDefault = true;

                //ensure No other car thinks it is the default
                foreach (CarInfo c in cars)
                    if (c != defaultCar && c.IsDefault)
                        c.IsDefault = false;
            }
        }

        public ObservableCollection<CarInfo> Cars
        {
            get => cars;
        }

        void SubscribeToCar(CarInfo car)
        {
            car.IsDefaultChangeEvent += OnCarMadeDefault;
            car.PropertyChanged += OnCarPropertyChanged;
        }

        void UnsubscribeToCar(CarInfo car)
        {
            car.IsDefaultChangeEvent -= OnCarMadeDefault;
            car.PropertyChanged -= OnCarPropertyChanged;
        }

        void OnCarMadeDefault(object sender, EventArgs e)
        {
            CarInfo c = sender as CarInfo;

            //c is the new default (set outside this class)
            if (c.IsDefault && c != defaultCar)
                DefaultCar = c;
            //c is no longer the default (set outside this class)
            else if (!c.IsDefault && c == defaultCar)
            {
                DefaultCar = null;
                if (cars.Count > 0) DefaultCar = cars[0];
            }
        }

        void OnCarPropertyChanged(object o, EventArgs e)
        {
            OnPropertyChanged("Car");
        }

        void OnCarsModified(object o, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Move) return;

            if (args.OldItems != null)
            {
                foreach (object _c in args.OldItems)
                {
                    CarInfo c = _c as CarInfo;
                    c.IsDefaultChangeEvent -= OnCarMadeDefault;
                    c.PropertyChanged -= OnCarPropertyChanged;

                    if (c == defaultCar) defaultCar = null;
                }
            }

            if (args.NewItems != null)
            {
                foreach (object _c in args.NewItems)
                {
                    CarInfo c = _c as CarInfo;

                    bool isDefault = c.IsDefault;

                    c.IsDefault = false;
                    c.IsDefaultChangeEvent += OnCarMadeDefault;
                    c.PropertyChanged += OnCarPropertyChanged;

                    if (isDefault)
                        c.IsDefault = isDefault;
                }
            }

            if (defaultCar == null && cars.Count > 0) DefaultCar = cars[0];

            OnPropertyChanged("Cars");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
