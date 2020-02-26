using System;
using System.Collections.Generic;
using TagRides.Shared.UserProfile;

using Xamarin.Forms;

namespace TagRides.NewUser
{
    public partial class PhoneInsert : ContentPage
    {
        ProfileCreationController controller;

        public PhoneInsert(ProfileCreationController controller, UserInfo userInfo)
        {
            InitializeComponent();

            BindingContext = userInfo;
            this.controller = controller;
            
            Appearing += (e, b) => entry.Focus();
        }

        void OnEntryComplete(object sender, EventArgs e)
        {
            AttemptToAdvance();
        }

        void OnNext(object sender, EventArgs e)
        {
            AttemptToAdvance();
        }

        async void AttemptToAdvance()
        {
            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                await DisplayAlert("Form Incomplete",
                                   "Please enter your phone number. Providing it is important for a secure experience.",
                                   "Ok");
                return;
            }

            controller.NextPage();
        }
    }
}
