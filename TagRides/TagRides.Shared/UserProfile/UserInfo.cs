using System;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TagRides.Shared.UserProfile
{
    /// <summary>
    /// Holds all basic profile information, and references to Driver and GameInfo
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class UserInfo : INotifyPropertyChanged
    {
        [JsonProperty]
        string nameFirst;
        [JsonProperty]
        string nameLast;
        [JsonProperty]
        string phoneNumber;
        [JsonProperty]
        string emailAddress;

        [JsonProperty]
        DriverInfo driverInfo;

        //TODO Shouldn't trigger any OnPropertyChangeds if the value doesn't actually change
        
        public string NameFirst
        {
            get => nameFirst;

            set
            {
                nameFirst = value;
                OnPropertyChanged("NameFirst");
            }
        }

        public string NameLast
        {
            get => nameLast;

            set
            {
                nameLast = value;
                OnPropertyChanged("NameLast");
            }
        }

        public string PhoneNumber
        {
            get => phoneNumber;

            set
            {
                phoneNumber = value;
                OnPropertyChanged("PhoneNumber");
            }
        }

        public string EmailAddress
        {
            get => emailAddress;

            set
            {
                emailAddress = value;
                OnPropertyChanged("EmailAddress");
            }
        }

        public DriverInfo DriverInfo
        {
            get => driverInfo;

            set
            {
                if (driverInfo != null) driverInfo.PropertyChanged -= OnDriverPropertyChanged;
                driverInfo = value;
                if (driverInfo != null) driverInfo.PropertyChanged += OnDriverPropertyChanged;
                OnPropertyChanged("DriverInfo");
            }
        }
        
        public string UserId
        {
            get
            {
                return emailAddress.Substring(0, emailAddress.IndexOf('@'));
            }
        }

        void OnDriverPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("DriverInfo." + e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
