using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.ViewUtilities;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.EditViews
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TripPicker : ContentView
	{
        public event Action StartTapped;
        public event Action EndTapped;

		public TripPicker ()
		{
			InitializeComponent ();
		}

        async void OnStartTapped(object o, EventArgs e)
        {
            await AnimationUtilites.Press(o as View);
            StartTapped?.Invoke();
        }

        async void OnEndTapped(object o, EventArgs e)
        {
            await AnimationUtilites.Press(o as View);
            EndTapped?.Invoke();
        }
	}
}