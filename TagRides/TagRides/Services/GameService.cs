using System;
using System.Threading.Tasks;
using TagRides.Shared.Utilities;
using TagRides.Utilities;

namespace TagRides.Services
{
    public class GameService
    {
        public static GameService Instance { get; } = new GameService();

        public async Task<bool> PostRatingAsync(string userId, double rating)
        {
            Uri requestUri = new Uri(App.Current.ServerAddress, "api/game/rating")
                .AddParameter("userId", userId)
                .AddParameter("rating", rating.ToString());

            return await ServiceUtilities.SendSimpleHttpPostRequest(requestUri);
        }

        public async Task<bool> PostFactionAsync(string userId, string factionName)
        {
            Uri requestUri = new Uri(App.Current.ServerAddress, "api/game/faction")
                .AddParameter("userId", userId)
                .AddParameter("factionName", factionName);

            return await ServiceUtilities.SendSimpleHttpPostRequest(requestUri);
        }
        
        public async Task<bool> PostRemoveFactionAsync(string userId, bool canChangeAgain)
        {
            Uri requestUri = new Uri(App.Current.ServerAddress, "api/game/remove-faction")
                .AddParameter("userId", userId)
                .AddParameter("canChangeAgain", canChangeAgain.ToString());

            return await ServiceUtilities.SendSimpleHttpPostRequest(requestUri);
        }
    }
}
