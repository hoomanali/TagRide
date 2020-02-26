using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.Main.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage
	{
		public SettingsPage ()
		{
			InitializeComponent ();

            themeSwitch.IsToggled = !App.Current.IsLightTheme;
		}

        private void ThemeSwitchToggled(object sender, ToggledEventArgs e)
        {
            if (e.Value)
                App.Current.SetDarkTheme();
            else
                App.Current.SetLightTheme();
        }
    }
}