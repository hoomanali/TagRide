using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.AppData;
using TagRides.Shared.Utilities;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.Game.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FactionIconView : ContentView
    {
        #region Bindable Property Declarations

        public static BindableProperty FactionProperty
            = BindableProperty.Create(
                nameof(Faction),
                typeof(FactionProperties),
                typeof(FactionIconView),
                propertyChanged: OnFactionPropertyChanged);

        static void OnFactionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FactionIconView view = bindable as FactionIconView;
            FactionProperties properties = newValue as FactionProperties;

            if (properties != null)
            {
                view.image.Source = ImageSource.FromUri(TagRidePropertyUtils.FactionIconUri(App.Current.TagRideProperties.Value, properties));
                view.label.Text = properties.Name;
            }
            else
            {
                view.image.Source = null;
                view.label.Text = "faction";
            }
        }

        #endregion

        public FactionIconView()
		{
			InitializeComponent();
		}

        public FactionProperties Faction
        {
            get => GetValue(FactionProperty) as FactionProperties;
            set => SetValue(FactionProperty, value);
        }
	}
}