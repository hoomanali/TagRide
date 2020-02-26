using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TagRides.Login
{
    public static class LoginUtils
    {
        // Check if a user is logged in
        // Return true if logged in.
        // Return false if not logged in.
        public static bool IsUserLoggedIn()
        {
            return Application.Current.Properties.ContainsKey("userEmail")
                && Application.Current.Properties["userEmail"] != null;
        }

        // Sets the userLoggedIn flag to true.
        public static void SetUserLoggedIn()
        {
            if (Application.Current.Properties.ContainsKey("userEmail"))
                Application.Current.Properties["userEmail"] = App.Current.UserInfo.EmailAddress;
            else
                Application.Current.Properties.Add("userEmail", App.Current.UserInfo.EmailAddress);

            App.Current.SavePropertiesAsync();
        }

        // Sets the userLoggedIn flag to false.
        public static void SetUserLoggedOut()
        {
            Application.Current.Properties.Remove("userEmail");
            App.Current.SavePropertiesAsync();
        }

        /// <summary>
        /// Gets the logged in user email.
        /// </summary>
        /// <returns>The logged in user email.</returns>
        public static string LoggedInUserEmail()
        {
            string userName;
            userName = Application.Current.Properties["userEmail"].ToString();
            return userName;
        }
    }
}
