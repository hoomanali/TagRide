using System;
using System.ComponentModel;
using System.Collections.Concurrent;
using TagRides.Shared.Game;
using TagRides.Shared.Utilities;
using Newtonsoft.Json;

namespace TagRides.Shared.UserProfile
{
    /// <summary>
    /// Information pertaining to game features
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class GameInfo : INotifyPropertyChanged
    {
        public Rating Rating
        {
            get => rating;
            set
            {
                rating = value;
                OnPropertyChanged(nameof(Rating));
            }
        }

        public PointTracker Level
        {
            get => level;
            set
            {
                level = value;
                OnPropertyChanged(nameof(Level));
            }
        }

        public PointTracker Kingdom
        {
            get => kingdom;
            set
            {
                kingdom = value;
                OnPropertyChanged(nameof(Kingdom));
            }
        }

        public GameInventory Inventory
        {
            get => inventory;
            set
            {
                inventory = value;
                OnPropertyChanged(nameof(Inventory));
            }
        }

        public string Faction
        {
            get => faction;
            set
            {
                faction = value;

                if (!hasHadFaction && !string.IsNullOrEmpty(faction)) hasHadFaction = true;

                OnPropertyChanged(nameof(Faction));
            }
        }

        public bool CanChangeFaction
        {
            get => canChangeFaction;
            set
            {
                canChangeFaction = value;
                OnPropertyChanged(nameof(CanChangeFaction));
            }
        }

        public bool HasHadFaction => hasHadFaction;

        [JsonProperty]
        Rating rating = new Rating();

        [JsonProperty]
        PointTracker level = new LinearPointTracker(1000, 1);
        [JsonProperty]
        PointTracker kingdom = new LinearPointTracker(1000, 0);
        [JsonProperty]
        GameInventory inventory = new GameInventory();

        [JsonProperty]
        string faction;
        [JsonProperty]
        bool hasHadFaction = false;
        [JsonProperty]
        bool canChangeFaction = true;
        
        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null) return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
