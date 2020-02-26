using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using TagRides.Shared.Utilities;

using Xamarin.Forms;

namespace TagRides.UserProfile
{
    /// <summary>
    /// Represents a user's profile image.
    /// Handle's getting the image, and what to do if the user has no image
    /// </summary>
    public class UserImage
    {
        //TODO maybe use a static getter to make sure their aren't two UserImages for the same user at a time
        public UserImage(string userId)
        {
            this.userId = userId;

            TaskUtilities.WaitSync(UpdateImage);
        }

        public ImageSource ImageSource
        {
            get
            {
                if (image != null)
                    return ImageSource.FromStream(GetImage);
                return App.Current.Resources["TemplateImage"] as ImageSource;
            }
        }

        public Stream GetImage()
        {
            return new MemoryStream(image, false);
        }

        /// <summary>
        /// Pull the user's image from the datastore if it exists.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateImage()
        {
            await TryGetUserImage();

            ImageUpdated?.Invoke(userId);
        }

        /// <summary>
        /// Updates the userInfo with a stream. Does not upload to datastore
        /// </summary>
        /// <param name="data"></param>
        public void UpdateImage(Stream data)
        {
            image = new byte[data.Length];
            data.Position = 0;
            data.Read(image, 0, (int)data.Length);
            data.Position = 0;
            
            ImageUpdated?.Invoke(userId);
        }

        async Task TryGetUserImage()
        {
            image = await App.Current.DataStore.GetPhoto(userId);
        }

        readonly string userId;
        byte[] image;

        /// <summary>
        /// Invoked when the image might have been updated. Passed string is the userId of the Image owner
        /// </summary>
        public event Action<string> ImageUpdated;
    }
}
