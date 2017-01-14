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
        public enum Theme { System, Light, Dark }

        private static MainPage mainPage;
        public static bool tvSafeArea = true;
        public static Theme theme = Theme.System;

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

            tvSafe.IsOn = tvSafeArea;
            if (theme == Theme.Light)
            {
                Light.IsChecked = true;
            }
            else if (theme == Theme.Dark)
            {
                Dark.IsChecked = true;
            }
            else
            {
                System.IsChecked = true;
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
            }
            saveSettings();
        }

        /// <summary>
        /// User selects a theme color
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeColor_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();

            if (((RadioButton)sender).Name == "System")
            {
                theme = Theme.System;
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
            else if (((RadioButton)sender).Name == "Light")
            {
                theme = Theme.Light;
                mainPage.RequestedTheme = ElementTheme.Light;
            }
            else if (((RadioButton)sender).Name == "Dark")
            {
                theme = Theme.Dark;
                mainPage.RequestedTheme = ElementTheme.Dark;
            }
            saveSettings();
        }

        /// <summary>
        /// Set the roaming settings for the application
        /// </summary>
        private void saveSettings()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            composite["TvSafeAreaOff"] = !tvSafe.IsOn;
            composite["Theme"] = theme.ToString();

            roamingSettings.Values["UserSettings"] = composite;
        }
    }
}
