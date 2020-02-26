using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagRides.Shared.AppData;

using Xamarin.Forms;

namespace TagRides.Game.Views
{
	public class FactionPicker : ContentView
	{
        public event Action<FactionProperties> SelectionChanged;

        public FactionIconView Selected { get; private set; } = null;

		public FactionPicker ()
		{
            StackLayout factions = new StackLayout
            {
                Orientation = StackOrientation.Horizontal
            };

            foreach (var faction in App.Current.TagRideProperties.Value.Factions)
            {
                FactionIconView view = new FactionIconView
                {
                    Faction = faction,
                    WidthRequest = 100
                };

                var tapGestureRecognizer = new TapGestureRecognizer();
                tapGestureRecognizer.NumberOfTapsRequired = 1;
                tapGestureRecognizer.Tapped += OnFactionViewTapped;

                view.GestureRecognizers.Add(tapGestureRecognizer);

                factions.Children.Add(view);
            }

            Content = new ScrollView
            {
                Content = factions,
                Orientation = ScrollOrientation.Horizontal
            };
        }
        
        public void ClearSelection()
        {
            if (Selected != null)
                Selected.BackgroundColor = Color.Transparent;
            Selected = null;
            SelectionChanged?.Invoke(null);
        }

        public Color SelectionColor
        {
            get => selectionColor;
            set
            {
                if (value == selectionColor) return;
                selectionColor = value;
                if (Selected != null)
                    Selected.BackgroundColor = value;
            }
        }

        void OnFactionViewTapped(object sender, EventArgs args)
        {
            if (sender == Selected) return;
            if (Selected != null)
                Selected.BackgroundColor = Color.Transparent;

            Selected = sender as FactionIconView;
            Selected.BackgroundColor = selectionColor;

            SelectionChanged?.Invoke(Selected.Faction);
        }

        Color selectionColor = Color.BurlyWood;
    }
}