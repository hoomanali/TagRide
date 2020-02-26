using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;
using TagRides.Shared.Utilities;

namespace TagRides.ViewUtilities
{
    public class NameToFactionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string factionName = value as string;
            return App.Current.TagRideProperties.Value.GetFaction(factionName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
