using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;

namespace TagRides.Shared.Game
{
    /// <summary>
    /// Represents a rating on a scale from 0 - 1
    /// </summary>
    [JsonObject]
    public class Rating : INotifyPropertyChanged
    {
        [JsonProperty]
        public double Current { get; private set; } = 1;
        [JsonProperty]
        public int TotalRatings { get; private set; } = 1;

        public void GiveRating(double rating)
        {
            if (rating < 0 || rating > 1)
                throw new Exception("Rating not in bounds.");

            double currentWeighted = Current * TotalRatings;
            Current = (currentWeighted + rating) / (++TotalRatings);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalRatings)));
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
