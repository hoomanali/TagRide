
using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Plugin.CurrentActivity;
using Plugin.Permissions;

namespace TagRides.Droid
{
    [Activity(Label = "TagRides", 
              Icon = "@mipmap/icon", 
              Theme = "@style/MainTheme", 
              MainLauncher = true, 
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
              ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Needed for Xam.Plugin.Geolocator.
            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            VerifyPermissions();
            TK.CustomMap.Droid.TKGoogleMaps.Init(this, savedInstanceState);

            global::Xamarin.Auth.Presenters.XamarinAndroid.AuthenticationConfiguration.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            // Needed for Xam.Plugin.Geolocator.
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        // Verify what permissions the device has
        // Ask to set permissions if not set by user
        public void VerifyPermissions()
        {
            // Permission IDs, set a different ID for each permission.
            int REQUEST_LOCATION = 0;

            // Check each permission and request permission if not already granted.
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOCATION);
            }
            else
            {
                // Do nothing, permission is already granted.
            }
        }
    }
}
