using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TagRides.Places.Autocomplete
{
    /// <summary>
    /// A response from the Google Places API.
    /// </summary>
    public class AutocompleteResponse
    {
        [JsonProperty("status")]
        public string Status { get; private set; }

        public IReadOnlyList<Prediction> Predictions => predictions;

        [JsonProperty("predictions")]
        List<Prediction> predictions;
    }

    public class Prediction
    {
        [JsonProperty("description")]
        public string Description { get; private set; }

        [JsonProperty("place_id")]
        public string PlaceId { get; private set; }
    }
}
