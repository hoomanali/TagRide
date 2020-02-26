using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.EditViews
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditStringView : ContentView
	{
        public EditStringView(Object bindingContext, string property, string propertyTitle, IValueConverter converter = null)
        {
            InitializeComponent();

            BindingContext = bindingContext;

            propertyLabel.Text = propertyTitle;

            propertyEntry.SetBinding(Entry.TextProperty, property, BindingMode.TwoWay, converter);
        }

        public EditStringView (Object bindingContext, string property, string propertyTitle, Keyboard keyboard, IValueConverter converter = null)
		{
			InitializeComponent();

            BindingContext = bindingContext;

            propertyLabel.Text = propertyTitle;

            propertyEntry.SetBinding(Entry.TextProperty, property, BindingMode.TwoWay, converter);
            propertyEntry.Keyboard = keyboard;
		}
	}
}