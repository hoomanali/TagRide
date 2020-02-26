using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace TagRides.ViewUtilities
{
    class PhoneNumberPrettyConverter : IValueConverter
    {
        string FormatLastSeven(string number)
        {
            if (string.IsNullOrEmpty(number))
                return "";

            string start = null, end = null;

            if (number.Length > 3)
            {
                start = number.Substring(0, 3);
                end = number.Substring(3, number.Length - 3);
            }
            else
            {
                start = number.Substring(0, number.Length);
            }

            string output = start;
            if (!string.IsNullOrEmpty(end))
                output += "-" + end;

            return output;
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string unformattedString = value as string;

            if (string.IsNullOrEmpty(unformattedString))
                return "";
            if (unformattedString.Length > 10)
                return unformattedString;

            if (unformattedString.Length <= 7)
                return FormatLastSeven(unformattedString);

            return "(" 
                + unformattedString.Substring(0, 3) 
                + ") " 
                + FormatLastSeven(unformattedString.Substring(3, unformattedString.Length - 3));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formattedString = value as string;

            string outputString = "";

            foreach (char c in formattedString)
                if (char.IsDigit(c))
                    outputString += c;

            return outputString;
        }
    }
}
