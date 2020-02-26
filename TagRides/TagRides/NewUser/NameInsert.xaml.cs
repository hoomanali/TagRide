using System;
using System.Collections.Generic;
using TagRides.Shared.UserProfile;

using Xamarin.Forms;

namespace TagRides.NewUser
{
    //TODO All the profile creation pages should be subclasses, as they share a lot of logic, and design
    public partial class NameInsert : ContentPage
    {
        ProfileCreationController controller;

        public NameInsert(ProfileCreationController controller, UserInfo userInfo)
        {
            InitializeComponent();

            BindingContext = userInfo;
            this.controller = controller;

            Appearing += (e, b) => firstEntry.Focus();
        }

        void OnFirstCompleted(object sender, System.EventArgs e)
        {
            secondEntry.Focus();
        }

        void OnSecondCompleted(object sender, System.EventArgs e)
        {
            AttemptToAdvance();
        }

        void OnNext(object sender, System.EventArgs e)
        {
            AttemptToAdvance();
        }

        async void AttemptToAdvance()
        {
            if (string.IsNullOrWhiteSpace(firstEntry.Text)
                || string.IsNullOrWhiteSpace(secondEntry.Text))
            {
                await DisplayAlert("Form Incomplete", 
                                   "Please enter both your first and last name. Providing a name is important for a secure experience.", 
                                   "Ok");
                return;
            }
            
            controller.NextPage();
        }
    }
}
