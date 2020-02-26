using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TagRides.Shared.Game;
using TagRides.Game.Views;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TagRides.Shared.AppData;
using TagRides.Services;

namespace TagRides.UserProfile.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class GamePage : ContentPage
	{
		public GamePage ()
		{
			InitializeComponent ();

            BindingContext = App.Current.GameInfo;
        }

        //Require two taps to make it a little harder to try to change factions
        async void OnFactionDoubleTapped(object o, EventArgs a)
        {
            if (!App.Current.GameInfo.CanChangeFaction)
            {
                await DisplayAlert("Can't change Faction", "Sorry, you cannot change Factions more than once.", "Ok");
                return;
            }

            if (await DisplayAlert(
                "Change Faction?",
                "You can only change factions once",
                "Change",
                "Nevermind"))
            {
                if (!await GameService.Instance.PostRemoveFactionAsync(App.Current.UserInfo.UserId, false))
                    await DisplayAlert("Error", "Failed to change faction", "Ok");
                else
                {
                    App.Current.GameInfo.Faction = null;
                    App.Current.GameInfo.CanChangeFaction = false;
                }
            }
        }

        async void OnFactionSelected(FactionProperties faction)
        {
            if (faction == null) return;

            string warningText;
            if (!App.Current.GameInfo.HasHadFaction)
                warningText = "You can only change your faction once after this.";
            else
                warningText = "You will not be able to change your faction after this.";

            if (await DisplayAlert(
                "Faction Selected!",
                $"You have chosen the faction {faction.Name}! {warningText} Are you sure this is your choice?",
                "Yes!",
                "No"))
            {
                if (!await GameService.Instance.PostFactionAsync(App.Current.UserInfo.UserId, faction.Name))
                    await DisplayAlert("Error", "Failed to set faction", "Ok");
                else
                    App.Current.GameInfo.Faction = faction.Name;

                //TODO add loading wheel while faction updates
            }

            factionPicker.ClearSelection();
        }
    }
}