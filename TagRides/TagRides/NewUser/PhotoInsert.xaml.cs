using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageCircle.Forms.Plugin.Abstractions;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;
using TagRides.Shared.UserProfile;

namespace TagRides.NewUser
{
    public partial class PhotoInsert : ContentPage
    {
        ProfileCreationController controller;

        public PhotoInsert(ProfileCreationController controller)
        {
            InitializeComponent();

            this.controller = controller;
            
            myCircleImage.Source = App.Current.Resources["TemplateImage"] as ImageSource;
        }

        private void HyperlinkButton_Click5(object sender, System.EventArgs e)
        {
            controller.NextPage();
        }

        private async void UploadPictureClicked(object sender, System.EventArgs e)
        {
            await DisplayAlert ("Notice", "Profile pictures should be a clear unobstructed photo of your face, this is important to maintain feelings of safety in our TagRide community.","OK");
            SetMediaFileToCircle(await UploadPhoto(myCircleImage));
        }

        private async void TakePictureClicked(object sender, System.EventArgs e)
        {
            await DisplayAlert ("Notice", "Profile pictures should be a clear unobstructed photo of your face, this is important to maintain feelings of safety in our TagRide community.","OK");
            SetMediaFileToCircle(await TakePhoto(myCircleImage));
        }

        async Task<MediaFile> TakePhoto(CircleImage image)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable && !CrossMedia.Current.IsTakePhotoSupported)
            {
                await Application.Current.MainPage.DisplayAlert("No Camera", "No Camera available.", "OK");
                return null;
            }

            var mediaOptions = new StoreCameraMediaOptions
            {
                SaveToAlbum = false,
                AllowCropping = true
            };

            return await CrossMedia.Current.TakePhotoAsync(mediaOptions);
        }
        
        async Task<MediaFile> UploadPhoto(CircleImage image)
        {
            await CrossMedia.Current.Initialize();
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await Application.Current.MainPage.DisplayAlert("Permission Issue", "Access to Photos Denied", "OK");
                return null;

            }
            var mediaOptions = new PickMediaOptions();
            return await CrossMedia.Current.PickPhotoAsync(mediaOptions);
        }

        void SetMediaFileToCircle(MediaFile file)
        {
            if (file == null) return;
            myCircleImage.Source = ImageSource.FromStream(file.GetStream);
            
            controller.SetImage(file.GetStream());
        }
    }
}
