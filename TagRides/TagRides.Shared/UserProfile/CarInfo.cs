using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace TagRides.Shared.UserProfile
{
    /// <summary>
    /// A Car's information
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CarInfo : INotifyPropertyChanged
    {
        [JsonProperty]
        bool isDefault;

        [JsonProperty]
        string name;
        [JsonProperty]
        string make;
        [JsonProperty]
        string model;
        [JsonProperty]
        string plate;

        //the capacity that will be auto filled when a driver sets up a ride.
        //this may be changed on a ride to ride basis
        [JsonProperty]
        int defaultCapacity = 1;

        //IMAGE TODO

        public event EventHandler IsDefaultChangeEvent;

        public bool IsDefault
        {
            get => isDefault;
            set
            {
                if (value == isDefault) return;

                isDefault = value;
                OnPropertyChanged("IsDefault");
                
                IsDefaultChangeEvent?.Invoke(this, new EventArgs());
            }
        }

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public string Make
        {
            get => make;
            set
            {
                make = value;
                OnPropertyChanged("Make");
            }
        }

        public string Model
        {
            get => model;
            set
            {
                model = value;
                OnPropertyChanged("Model");
            }
        }

        public string Plate
        {
            get => plate;
            set
            {
                plate = value;
                OnPropertyChanged("Plate");
            }
        }

        public int DefaultCapacity
        {
            get => defaultCapacity;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException();

                defaultCapacity = value;
                OnPropertyChanged("DefaultCapacity");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return name;
        }
    }
}
