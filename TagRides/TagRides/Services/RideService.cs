using System;
using System.Net.Http;
using System.Threading.Tasks;
using TagRides.Shared.Geo;
using Newtonsoft.Json;
using TagRides.Shared.RideData;
using TagRides.Shared.RideData.Status;
using TagRides.Shared.Utilities;
using TagRides.Utilities;

namespace TagRides.Services
{
    /// <summary>
    /// Singleton class for using the methods in the RidesController class in the
    /// server project.
    /// </summary>
    public class RideService
    {
        public static RideService Instance { get; } = new RideService();

        public async Task<string> PostRideRequestAsync(string userId, RideRequest rideRequest)
        {
            var uri = new Uri(App.Current.ServerAddress, "api/rides/ride-request")
                .AddParameter("userId", userId);
            var content = new StringContent(JsonConvert.SerializeObject(rideRequest), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await App.Current.HttpClient.PostAsync(uri, content);

                Console.WriteLine($"Response code: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Response body: {await response.Content.ReadAsStringAsync()}");
                    return response.Headers.Location.OriginalString;
                }
                else
                {
                    Console.WriteLine($"Reason phrases: {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got exception: {e}");
                return null;
            }
        }

        public async Task<string> PostRideOfferAsync(string userId, RideOffer rideOffer)
        {
            var uri = new Uri(App.Current.ServerAddress, "api/rides/ride-offer")
                .AddParameter("userId", userId);
            var content = new StringContent(JsonConvert.SerializeObject(rideOffer), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await App.Current.HttpClient.PostAsync(uri, content);

                Console.WriteLine($"Response code: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Response body: {await response.Content.ReadAsStringAsync()}");
                    return response.Headers.Location.OriginalString;
                }
                else
                {
                    Console.WriteLine($"Reason phrases: {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got exception: {e}");
                return null;
            }
        }

        public async Task<bool> PostConfirmAsync(string userId, string pendingRideId)
        {
            var uri = new Uri(App.Current.ServerAddress, "api/rides/pending-ride/confirm")
                .AddParameter("userId", userId)
                .AddParameter("pendingRideId", pendingRideId);

            return await ServiceUtilities.SendSimpleHttpPostRequest(uri);
        }

        public async Task<bool> PostRequestCancelAsync(string userId, string requestId)
        {
            var uri = new Uri(App.Current.ServerAddress, "api/rides/request/cancel")
                .AddParameter("userId", userId)
                .AddParameter("requestId", requestId);

            return await ServiceUtilities.SendSimpleHttpPostRequest(uri);
        }

        public async Task<bool> PostActiveRideInRideAsync(string userId, string activeRideId)
        {
            return await PostActiveRideMethod(userId, activeRideId, "in-ride");
        }

        public async Task<bool> PostActiveRideFinishAsync(string userId, string activeRideId)
        {
            return await PostActiveRideMethod(userId, activeRideId, "finish");
        }

        public async Task<bool> PostActiveRideCancelAsync(string userId, string activeRideId)
        {
            return await PostActiveRideMethod(userId, activeRideId, "cancel");
        }

        async Task<bool> PostActiveRideMethod(string userId, string activeRideId, string resource)
        {
            var uri = new Uri(App.Current.ServerAddress, $"api/rides/active-ride/{resource}")
                .AddParameter("userId", userId)
                .AddParameter("activeRideId", activeRideId);

            return await ServiceUtilities.SendSimpleHttpPostRequest(uri);
        }

        public async Task<RideRelatedRequestStatus> PollRideRelatedRequestAsync(string userId, string requestId)
        {
            return await App.Current.DataStore.GetRideRelatedRequestStatus(userId, requestId);
        }

        public async Task<PendingRideStatus> PollPendingRideStatusAsync(string pendingRideId)
        {
            return await App.Current.DataStore.GetPendingRideStatus(pendingRideId);
        }

        public async Task<string> PostFakeRideRequestAsync(GeoCoordinates source, GeoCoordinates destination)
        {
            Random rand = new Random();
            string userId = $"fakepeshin-{rand.Next()}";

            return await PostRideRequestAsync(userId, new RideRequest(
                new Trip(DateTime.Now, source, destination),
                new RequestGameElements(null)));
        }

        public async Task<(string,string)> PostFakeRideOfferAsync(GeoCoordinates source, GeoCoordinates destination)
        {
            Random rand = new Random();
            string userId = $"fakepeshin-{rand.Next()}";

            return (await PostRideOfferAsync(userId,
                        new RideOffer(
                            new Trip(DateTime.Now, source, destination),
                            1000, 
                            new Shared.UserProfile.CarInfo(), 
                            1,
                            new RequestGameElements(null))),
                    userId);
        }
    }
}
