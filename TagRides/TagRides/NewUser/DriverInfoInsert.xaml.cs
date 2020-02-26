using System;
using System.Collections.Generic;
using TagRides.Shared.UserProfile;
using TagRides.UserProfile;
using TagRides.UserProfile.Views;
using Xamarin.Forms;

namespace TagRides.NewUser
{
    public partial class DriverInfoInsert : ContentPage
    {
        UserInfo userInfo;
        ProfileCreationController controller;


        public DriverInfoInsert(ProfileCreationController controller, UserInfo userInfo)
        {
            InitializeComponent();

            this.userInfo = userInfo;
            this.controller = controller;
        }

        private async void HyperlinkButton_ClickedYes(object sender, System.EventArgs e)
        {
            userInfo.DriverInfo = new DriverInfo();

            Page carCreationPage = new CarEditPage(userInfo.DriverInfo);

            carCreationPage.Disappearing += (s, a) =>
            {
                controller.CreationFlowComplete();
            };

            await Navigation.PushAsync(carCreationPage);
        }

        private void HyperlinkButton_ClickedNo(object sender, System.EventArgs e)
        {
            controller.CreationFlowComplete();
        }
    }
}
