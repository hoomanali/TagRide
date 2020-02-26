using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagRides.Shared.Game;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TagRides.Game.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class GameItemPicker : ContentView
	{
        public GameInventory Inventory
        {
            get => inventoryView.Inventory;
            set => inventoryView.Inventory = value;
        }

        public Action<GameItem> ItemPicked;
        public Action Cancel;

        public GameItemPicker()
		{
			InitializeComponent();

            inventoryView.SelectionChanged += OnSelectionChanged;
		}

        void OnSelectionChanged(GameItem selectedView)
        {
            selectButton.IsEnabled = selectedView != null;
        }

        void OnSelectClicked(object sender, EventArgs e)
        {
            ItemPicked?.Invoke(inventoryView.Selected.Item);
        }

        void OnCancelClicked(object sender, EventArgs e)
        {
            Cancel?.Invoke();
        }
    }
}