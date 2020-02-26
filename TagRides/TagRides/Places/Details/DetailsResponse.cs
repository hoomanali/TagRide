using Newtonsoft.Json;

namespace TagRides.Places.Details
{
    /// <summary>
    /// Response to a Place Details query in the Google Places API.
    /// </summary>
    public class DetailsResponse
    {
        [JsonProperty("result")]
        public Result Result { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }
    }

    public class Result
    {
        [JsonProperty("geometry")]
        public Geometry Geometry { get; private set; }
    }

    public class Geometry
    {
        [JsonProperty("location")]
        public Location Location { get; private set; }

        [JsonProperty("viewport")]
        public Viewport Viewport { get; private set; }
    }

    public class Viewport
    {
        [JsonProperty("northeast")]
        public Location Northeast { get; private set; }

        [JsonProperty("southwest")]
        public Location Southwest { get; private set; }
    }

    public class Location
    {
        [JsonProperty("lat")]
        public double Latitude { get; private set; }

        [JsonProperty("lng")]
        public double Longitude { get; private set; }
    }
}
