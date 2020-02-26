using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using TagRides.Shared.DataStore;
using TagRides.Shared.Utilities;
using TagRides.Shared.AppData;
using System.Threading.Tasks;

namespace TagRides.Server
{
    public class Program
    {
        public class ServerErrorHandler : IErrorHandler
        {
            public void HandleError(Exception e)
            {
                LogError(e.ToString());
            }
        }

        public static IErrorHandler ErrorHandler = new ServerErrorHandler();

        // Our Google Maps API key. This is restricted to the static IP we
        // use for our server, so it is safe to upload to version control.
        public static string GoogleApiKey = "AIzaSyCaUOfkUIpVli30UigsbrZWZ79Aa9Kn8SQ";

        public static TagRideDataStore DataStore = new TagRideDataStore
        {
            DataStore = new AzureDataStore()
        };

        public static TagRideProperties TagRideProperties => tagRideProperties;

        public static IEnumerable<string> GetErrorMessages()
        {
            return errorMessages.ToArray();
        }

        public static IEnumerable<string> GetAndResetErrorMessages()
        {
            var errors = errorMessages.ToArray();
            errorMessages.Clear();
            return errors;
        }

        public static void LogError(string message)
        {
            Console.WriteLine(message);

            errorMessages.Enqueue(message);
        }

        public static void Main(string[] args)
        {
            SetUpTagRideProperties().FireAndForgetAsync(ErrorHandler);

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

        static async Task SetUpTagRideProperties()
        {
            tagRideProperties = await DataStore.GetTagRideProperties();

            if (tagRideProperties == null)
            {
                //Use the default properties
                tagRideProperties = new TagRideProperties();
                DataStore.PostTagRideProperties(tagRideProperties).FireAndForgetAsync(ErrorHandler);
            }
        }

        static readonly ConcurrentQueue<string> errorMessages = new ConcurrentQueue<string>();
        static TagRideProperties tagRideProperties;
    }
}
