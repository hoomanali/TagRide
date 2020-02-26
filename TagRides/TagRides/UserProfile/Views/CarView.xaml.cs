using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.UserProfile;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.UserProfile.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CarView : ContentView
	{
        public CarInfo Car { get; private set; }

		public CarView (CarInfo carInfo)
		{
			InitializeComponent ();

            BindingContext = Car = carInfo;
		}
	}
}