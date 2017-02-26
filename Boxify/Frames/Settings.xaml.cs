using System;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
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
        public enum Playbacksource { Spotify, YouTube }

        private static MainPage mainPage;

        public static bool tvSafeArea = true;
        public static Theme theme = Theme.System;
        public static Playbacksource playbackSource = Playbacksource.Spotify;
        public static bool repeatEnabled = false;
        public static double volume = 100;

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

            // color theme
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
            TvSafeArea.IsOn = tvSafeArea;

            // playback source
            if (playbackSource == Playbacksource.YouTube)
            {
                YouTube.IsChecked = true;
            }
            else
            {
                Spotify.IsChecked = true;
            }

            // version
            Version.Text = string.Format("{0}.{1}.{2}.{3}",
                                         Package.Current.Id.Version.Major,
                                         Package.Current.Id.Version.Minor,
                                         Package.Current.Id.Version.Build,
                                         Package.Current.Id.Version.Revision);
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
                tvSafeArea = toggleSwitch.IsOn;
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
        /// User clicks an options for Playback Source
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playbacksource_Click(object sender, RoutedEventArgs e)
        {
            if (((RadioButton)sender).Name == "Spotify")
            {
                playbackSource = Playbacksource.Spotify;
            }
            else if (((RadioButton)sender).Name == "YouTube")
            {
                playbackSource = Playbacksource.YouTube;
            }
            saveSettings();
        }

        /// <summary>
        /// Set the roaming settings for the application
        /// </summary>
        public static void saveSettings()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
            composite["TvSafeAreaOff"] = !tvSafeArea;
            composite["Theme"] = theme.ToString();
            composite["PlaybackSource"] = playbackSource.ToString();
            composite["RepeatEnabled"] = repeatEnabled.ToString();
            composite["Volume"] = volume.ToString();

            roamingSettings.Values["UserSettings"] = composite;
        }

        /// <summary>
        /// User selects to go to browser for repository
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Repo_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/aboe026/boxify"));
        }

        /// <summary>
        /// User selects to go to browser for Spotify support page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SpotifyGitHub_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/spotify/web-api/issues/57"));
        }

        /// <summary>
        /// User selects to go to store to rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Package.Current.Id.FamilyName)));
        }
    }
}
