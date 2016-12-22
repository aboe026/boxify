using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        private static MainPage mainPage;

        /// <summary>
        /// Main constructor
        /// </summary>
        public Settings()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }

            // tv safe area
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["UserSettings"];
            if (composite != null)
            {
                object val = composite["TvSafeAreaOff"];
                if ((bool)composite["TvSafeAreaOff"])
                {
                    tvSafe.IsOn = false;
                }
                else
                {
                    tvSafe.IsOn = true;
                }
            }
            else
            {
                tvSafe.IsOn = true;
            }
        }

        /// <summary>
        /// User wishes to toggle the tv safe area margins
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvSafe_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn)
                {
                    mainPage.safeAreaOn();   
                }
                else
                {
                    mainPage.safeAreaOff();
                }
                setTvSafeSetting(toggleSwitch.IsOn);
            }
        }

        /// <summary>
        /// Set the roaming settings for the tv safe area
        /// </summary>
        /// <param name="enabled"></param>
        private static void setTvSafeSetting(bool enabled)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            composite["TvSafeAreaOff"] = !enabled;

            roamingSettings.Values["UserSettings"] = composite;
        }
    }
}
