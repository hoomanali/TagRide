using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagRides.Services;
using Xamarin.Forms;

namespace TagRides.Main.Views
{
    public partial class ServerConfigPage : ContentPage
    {
        public ServerConfigPage()
        {
            InitializeComponent();

            AddressField.Text = App.Current.ServerAddress?.ToString() ?? "";
        }

        async void HandleServerAddressEntered(object sender, EventArgs e)
        {
            try
            {
                App.Current.ServerAddress = new Uri(AddressField.Text);
                await Ping();
            }
            catch (FormatException)
            {
                ErrorText.IsVisible = true;
                ErrorText.Text = "Incorrect format. (example: http://192.168.1.10:5000/)";
            }
        }

        void HandleServerAddressChanged(object sender, TextChangedEventArgs e)
        {
            ErrorText.IsVisible = false;
        }

        async void HandlePing(object sender, EventArgs e)
        {
            await Ping();
        }

        async Task Ping()
        {
            Animation ellipsisAnimation = new Animation(t => PingStatus.Text = "waiting" + new string('.', (int)(t * 4)));

            PingStatus.Text = "waiting";
            PingStatus.Animate("Ellipsis", ellipsisAnimation, 200, 800, null, null, () => true);

            bool success = await DataService.Instance.PingServerAsync();

            PingStatus.AbortAnimation("Ellipsis");
            PingStatus.Text = success ? "OK" : "Failed";
            PingStatus.TextColor = success ? Color.Green : Color.Red;
        }
    }
}
