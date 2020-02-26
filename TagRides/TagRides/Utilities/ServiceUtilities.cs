using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TagRides.Utilities
{
    public static class ServiceUtilities
    {
        public static async Task<bool> SendSimpleHttpPostRequest(Uri requestUri)
        {
            try
            {
                var response = await App.Current.HttpClient.PostAsync(requestUri, null);

                Console.WriteLine($"Response code: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"Reason phrases: {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got HTTP exception: {e}");
            }

            return false;
        }

        public static async Task<HttpContent> SendSimpleHttpGetRequest(Uri requestUri)
        {
            try
            {
                var response = await App.Current.HttpClient.GetAsync(requestUri);

                Console.WriteLine($"Response code: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Reason phrases: {response.ReasonPhrase}");
                    return null;
                }

                return response.Content;

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Got HTTP exception: {e}");
            }

            return null;
        }
    }
}
