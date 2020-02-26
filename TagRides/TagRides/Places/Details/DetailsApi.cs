using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using TagRides.Shared.Utilities;
using Newtonsoft.Json;

namespace TagRides.Places.Details
{
    /// <summary>
    /// Static class for making requests to Google's Place Details API.
    /// </summary>
    public static class DetailsApi
    {
        /// <summary>
        /// Makes a Place Details query for just the geometry of a given place.
        /// The "geometry" contains the place's location and possibly a viewport.
        /// </summary>
        /// <exception cref="HttpRequestException">Happens if the HTTP request fails.</exception>
        /// <exception cref="GoogleApiKeyException">Happens if the API key was bad.</exception>
        /// <returns>The place geometry or null.</returns>
        /// <param name="placeId">Place identifier.</param>
        /// <param name="apiKey">API key.</param>
        /// <param name="sessionToken">Session token.</param>
        public static async Task<DetailsResponse> GetPlaceGeometry(
            string placeId, string apiKey, string sessionToken)
        {
            Uri uri = new Uri("https://maps.googleapis.com/maps/api/place/details/json")
                .AddParameter("placeid", placeId)
                .AddParameter("key", apiKey)
                .AddParameter("sessiontoken", sessionToken)

                // Only request the geometry field to reduce cost.
                .AddParameter("fields", "geometry");

            // This may throw an HttpRequestException.
            HttpResponseMessage response = await App.Current.HttpClient.GetAsync(uri);

            DetailsResponse details = JsonConvert.DeserializeObject<DetailsResponse>(
                await response.Content.ReadAsStringAsync());

            if (details.Status != "OK")
            {
                if (details.Status == "REQUEST_DENIED")
                    throw new GoogleApiKeyException($"Api key {apiKey} was invalid.");

                // TODO: Consider "OVER_QUERY_LIMIT" and "ZERO_RESULTS" responses too.
                return null;
            }

            return details;
        }
    }
}
