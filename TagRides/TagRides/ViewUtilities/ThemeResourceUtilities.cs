using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    public static class ThemeResourceUtilities
    {
        public static ImageSource ToGameItemIcon(this string resource)
        {
            App app = App.Current;

            if (string.IsNullOrEmpty(resource))
                resource = app.TagRideProperties.Value.GameItemDefaultIcon;

            Uri uri = new Uri(app.TagRideProperties.Value.ThemeResourceBase + "gameItems/" + resource);

            return ImageSource.FromUri(uri);
        }
    }
}
