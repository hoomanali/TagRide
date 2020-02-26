using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TagRides.Shared.Geo;
using TagRides.Shared.Utilities;
using TagRides.Utilities;

namespace TagRides.Services
{
    /// <summary>
    /// Singleton class for using the methods in the DataController class in the
    /// server project.
    /// </summary>
    public class DataService
    {
        public static DataService Instance { get; } = new DataService();

        public async Task<bool> PostLocationAsync(string userId, GeoCoordinates location)
        {
            Uri requestUri = new Uri(App.Current.ServerAddress, $"api/data/location")
                .AddParameter("userId", userId);
            var content = new StringContent(JsonConvert.SerializeObject(location), Encoding.UTF8, "application/json");

            try
            {
                var response = await App.Current.HttpClient.PostAsync(requestUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Request not successful. Code {response.StatusCode}, phrase: {response.ReasonPhrase}");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got HTTP exception: {e}");
                return false;
            }
        }

        public async Task<bool> PostFakeLocationAsync(GeoCoordinates location)
        {
            Random rand = new Random();
            return await PostLocationAsync($"fakepeshin-{rand.Next()}", location);
        }

        public async Task<IEnumerable<GeoCoordinates>> GetPassengersAsync()
        {
            Uri requestUri = new Uri(App.Current.ServerAddress, $"api/data/passengers");

            try
            {
                var response = await App.Current.HttpClient.GetAsync(requestUri);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Request not successful. Code {response.StatusCode}, phrase: {response.ReasonPhrase}");
                    return new List<GeoCoordinates>();
                }
                else
                {
                    return JsonConvert.DeserializeObject<IEnumerable<GeoCoordinates>>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got HTTP exception: {e}");
                return new List<GeoCoordinates>();
            }
        }

        /// <summary>
        /// Attempts to ping the server. Returns true if a connection is made
        /// and the server responds successfully, and false if there is an error.
        /// </summary>
        /// <returns>The server async.</returns>
        public async Task<bool> PingServerAsync()
        {
            // These cases would cause the Uri constructor to throw an exception.
            if (App.Current.ServerAddress == null || !App.Current.ServerAddress.IsAbsoluteUri)
                return false;

            var requestUri = new Uri(App.Current.ServerAddress, "api/rides/ping");

            try
            {
                var response = await App.Current.HttpClient.GetAsync(requestUri);

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // This can happen on timeout, DNS failure, certificate failure, network failure.
                return false;
            }
        }

        /// <summary>
        /// Ping the server that the user's UserInfo has been updated
        /// </summary>
        /// <returns></returns>
        public async Task<bool> PostUserInfoUpdated(string userId)
        {
            var requestUri = new Uri(App.Current.ServerAddress, "api/data/userinfoupdate")
                .AddParameter("userId", userId);

            return await ServiceUtilities.SendSimpleHttpPostRequest(requestUri);
        }

        public async Task<IEnumerable<string>> GetPendingRideRequestIds(string userId)
        {
            return await GetUserStringList(userId, "api/data/pending-ride-request-ids");
        }

        public async Task<IEnumerable<string>> GetPendingRideOfferIds(string userId)
        {
            return await GetUserStringList(userId, "api/data/pending-ride-offer-ids");
        }

        public async Task<IEnumerable<string>> GetPendingRideIds(string userId)
        {
            return await GetUserStringList(userId, "api/data/pending-ride-ids");
        }

        public async Task<IEnumerable<string>> GetActiveRideIds(string userId)
        {
            return await GetUserStringList(userId, "api/data/active-ride-ids");
        }

        async Task<IEnumerable<string>> GetUserStringList(string userId, string resource)
        {
            var requestUri = new Uri(App.Current.ServerAddress, resource)
                .AddParameter("userId", userId);

            var content = await ServiceUtilities.SendSimpleHttpGetRequest(requestUri);
            if (content == null)
                return new string[] { };

            return JsonConvert.DeserializeObject<IEnumerable<string>>(await content.ReadAsStringAsync());
        }
    }
}
