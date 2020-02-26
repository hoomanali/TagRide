using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.AppData;
using TagRides.Game.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.NewUser
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FactionInsert : ContentPage
    {
        public FactionInsert(ProfileCreationController controller)
        {
            InitializeComponent();

            this.controller = controller;
        }

        async void OnWhyClicked(object sender, EventArgs args)
        {
            await DisplayAlert(
                "Factions?", 
                "With TagRide, you're doing more than just ride sharing! Align yourself with the faction that best represents you.", 
                "Ok...");
        }

        async void OnNextClicked(object sender, EventArgs args)
        {
            if (factionPicker.Selected == null)
            {
                if (await DisplayAlert("No faction selected!", "Please select a faction", "Ok", "I really don't want too"))
                    return;

                await DisplayAlert("Skipping faction selection", "You can select a faction later from your game profile!", "Ok");
            }
            else
            {
                controller.SetFaction(factionPicker.Selected.Faction.Name);
            }

            controller.NextPage();
        }

        readonly ProfileCreationController controller;
    }
}