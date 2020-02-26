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
    public partial class CarEditPage : ContentPage
    {
        bool isNew;
        CarInfo carInfo;
        DriverInfo driverInfo;

        /// <summary>
        /// Make a page to setup a new car
        /// </summary>
        /// <param name="driverInfo"></param>
        public CarEditPage(DriverInfo driverInfo)
        {
            InitializeComponent();

            this.driverInfo = driverInfo ?? throw new ArgumentNullException();
            isNew = true;

            carInfo = new CarInfo
            {
                Name = "Car_" + driverInfo.Cars.Count,
                DefaultCapacity = 1
            };

            Title = "New Car";

            SetUpPage();
		}

        /// <summary>
        /// Make a page to edit an existing car. There will be an option for the car to be removed.
        /// </summary>
        /// <param name="driverInfo"></param>
        /// <param name="car"></param>
        public CarEditPage(DriverInfo driverInfo, CarInfo car)
        {
            InitializeComponent();

            if (driverInfo == null || car == null) throw new ArgumentNullException();

            isNew = false;
            this.driverInfo = driverInfo;

            carInfo = car;

            Title = "Edit Car";
            cancelButton.Text = "Remove";

            SetUpPage();
        }

        void SetUpPage()
        {
            BindingContext = carInfo;

            mainStack.Children.Add(new EditViews.EditStringView(carInfo, "Name", "Name"));
            mainStack.Children.Add(new EditViews.EditStringView(carInfo, "Make", "Make"));
            mainStack.Children.Add(new EditViews.EditStringView(carInfo, "Model", "Model"));
            mainStack.Children.Add(new EditViews.EditStringView(carInfo, "Plate", "Plate #"));
            mainStack.Children.Add(new EditViews.EditStringView(carInfo, "DefaultCapacity", "Seats Available", Keyboard.Numeric));
        }

        void CancelClicked(object sender, EventArgs e)
        {
            if (!isNew)
                driverInfo.Cars.Remove(carInfo);

            NavigationPage np = Parent as NavigationPage;
            np.PopAsync();
        }

        //TODO Currently doesn't do anything for pre existing cars, should
        //have a local copy, and replace the old one when this is pressed,
        //and discard the changes otherwise
        private void ConfirmButtonClicked(object sender, EventArgs e)
        {
            if (isNew)
            {
                driverInfo.Cars.Add(carInfo);
                if (carInfo.IsDefault) driverInfo.DefaultCar = carInfo;
            }

            NavigationPage np = Parent as NavigationPage;
            np.PopAsync();
        }
    }
}