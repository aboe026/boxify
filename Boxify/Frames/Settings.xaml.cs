/*******************************************************************
Boxify - A Spotify client for Xbox One
Copyright(C) 2017 Adam Boe

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.If not, see<http://www.gnu.org/licenses/>.
*******************************************************************/

using Boxify.Controls.Announcements;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public enum Theme { System, Light, Dark }
        public enum PlaybackSource { Spotify, YouTube }

        public static bool tvSafeArea = true;
        public static Theme theme = Theme.System;
        public static PlaybackSource playbackSource = PlaybackSource.Spotify;
        public static bool repeatEnabled = false;
        public static bool shuffleEnabled = false;
        public static double volume = 100;
        public static string version = "";

        /// <summary>
        /// Main constructor
        /// </summary>
        public Settings()
        {
            this.InitializeComponent();
            MainPage.settingsPage = this;

            // tv safe area
            TvSafeArea.IsOn = tvSafeArea;

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

            // playback source
            if (playbackSource == PlaybackSource.YouTube)
            {
                YouTube.IsChecked = true;
            }
            else
            {
                Spotify.IsChecked = true;
            }

            // repeat
            RepeatToggle.Toggled -= RepeatToggle_Toggled;
            RepeatToggle.IsOn = repeatEnabled;
            RepeatToggle.Toggled += RepeatToggle_Toggled;

            // shuffle
            ShuffleToggle.Toggled -= ShuffleToggle_Toggled;
            ShuffleToggle.IsOn = shuffleEnabled;
            ShuffleToggle.Toggled += ShuffleToggle_Toggled;

            // version
            Version.Text = version;
        }

        /// <summary>
        /// User wishes to toggle the tv safe area margins
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TvSafe_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                tvSafeArea = toggleSwitch.IsOn;
                if (toggleSwitch.IsOn)
                {
                    App.mainPage.SafeAreaOn();
                }
                else
                {
                    App.mainPage.SafeAreaOff();
                }
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the TV Safe Area setting and update the UI
        /// </summary>
        /// <param name="enabled">True to enable the TV Safe Area, false to disable it</param>
        public void SetTvSafeUI(bool enabled)
        {
            SetTvSafe(enabled);
            TvSafeArea.IsOn = enabled;
            if (enabled)
            {
                if (App.mainPage != null)
                {
                    App.mainPage.SafeAreaOn();
                }
            }
            else
            {
                if (App.mainPage != null)
                {
                    App.mainPage.SafeAreaOff();
                }
            }
        }

        /// <summary>
        /// Change the Tv Safe Area setting
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetTvSafe(bool enabled)
        {
            tvSafeArea = enabled;
            SaveSettings();
        }

        /// <summary>
        /// User selects a theme color
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeColor_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton).Name == "System")
            {
                theme = Theme.System;
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
            else if ((sender as RadioButton).Name == "Light")
            {
                theme = Theme.Light;
                App.mainPage.RequestedTheme = ElementTheme.Light;
            }
            else if ((sender as RadioButton).Name == "Dark")
            {
                theme = Theme.Dark;
                App.mainPage.RequestedTheme = ElementTheme.Dark;
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the theme setting and adjust the UI
        /// </summary>
        /// <param name="newTheme">The theme to set the UI to</param>
        public void SetThemeUI(Theme newTheme)
        {
            SetTheme(newTheme);
            if (theme == Theme.System && App.mainPage != null)
            {
                System.IsChecked = true;
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
            else if (theme == Theme.Light && App.mainPage != null)
            {
                Light.IsChecked = true;
                App.mainPage.RequestedTheme = ElementTheme.Light;
            }
            else if (theme == Theme.Dark && App.mainPage != null)
            {
                Dark.IsChecked = true;
                App.mainPage.RequestedTheme = ElementTheme.Dark;
            }
        }

        /// <summary>
        /// Change the theme setting
        /// </summary>
        /// <param name="newTheme"></param>
        public static void SetTheme(Theme newTheme)
        {
            theme = newTheme;
            SaveSettings();
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
                playbackSource = PlaybackSource.Spotify;
            }
            else if (((RadioButton)sender).Name == "YouTube")
            {
                playbackSource = PlaybackSource.YouTube;
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the playback type and update the UI
        /// </summary>
        /// <param name="source">The source to get tracks from</param>
        public void SetPlaybackSourceUI(PlaybackSource source)
        {
            SetPlaybackSource(source);
            if (source == PlaybackSource.Spotify)
            {
                Spotify.IsChecked = true;
            }
            else if (source == PlaybackSource.YouTube)
            {
                YouTube.IsChecked = true;
            }
        }

        /// <summary>
        /// Change the playback type
        /// </summary>
        /// <param name="source"></param>
        public static void SetPlaybackSource(PlaybackSource source)
        {
            playbackSource = source;
            SaveSettings();
        }

        /// <summary>
        /// Change whether or not playback is repeated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepeatToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                SetRepeat(!repeatEnabled);
                App.playbackService.SetRepeat(repeatEnabled);
            }
        }

        /// <summary>
        /// Update the Repeat setting
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetRepeat(bool enabled)
        {
            repeatEnabled = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Update settings (including UI) with new Repeat value
        /// </summary>
        /// <param name="enabled"></param>
        public void SetRepeatUI(bool enabled)
        {
            SetRepeat(enabled);
            RepeatToggle.Toggled -= RepeatToggle_Toggled;
            RepeatToggle.IsOn = enabled;
            RepeatToggle.Toggled += RepeatToggle_Toggled;
        }

        /// <summary>
        /// Change the UI and call to playback
        /// </summary>
        public void ToggleRepeat()
        {
            RepeatToggle.IsOn = !RepeatToggle.IsOn;
        }

        /// <summary>
        /// Change whether or not playback is shuffled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShuffleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                SetShuffle(!shuffleEnabled);
                App.playbackService.SetShuffle(shuffleEnabled);
            }
        }

        /// <summary>
        /// Set the Shuffle setting
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetShuffle(bool enabled)
        {
            shuffleEnabled = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set the Shuffle settingand update the UI
        /// </summary>
        /// <param name="enabled"></param>
        public void SetShuffleUI(bool enabled)
        {
            SetShuffle(enabled);
            ShuffleToggle.Toggled -= ShuffleToggle_Toggled;
            ShuffleToggle.IsOn = enabled;
            ShuffleToggle.Toggled += ShuffleToggle_Toggled;
        }

        /// <summary>
        /// Change the UI and call to playback
        /// </summary>
        public void ToggleShuffle()
        {
            ShuffleToggle.IsOn = !ShuffleToggle.IsOn;
        }

        /// <summary>
        /// Set the roaming settings for the application
        /// </summary>
        public static void SaveSettings()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue
            {
                ["TvSafeAreaOff"] = !tvSafeArea,
                ["Theme"] = theme.ToString(),
                ["PlaybackSource"] = playbackSource.ToString(),
                ["RepeatEnabled"] = repeatEnabled.ToString(),
                ["ShuffleEnabled"] = shuffleEnabled.ToString(),
                ["Volume"] = volume.ToString()
            };
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
        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Package.Current.Id.FamilyName)));
        }

        /// <summary>
        /// Show announcements for first time users
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WelcomeConfigure_Click(object sender, RoutedEventArgs e)
        {
            List<UserControl> announcements = new List<UserControl>
            {
                new Welcome(),
                new TvMode(tvSafeArea),
                new ThemeMode(theme),
                new PlaybackMode(playbackSource),
                new PlaybackOptions(repeatEnabled, shuffleEnabled)
            };
            App.mainPage.ShowAnnouncements(announcements, this);
        }

        /// <summary>
        /// Show announcement for new shuffle feature 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            List<UserControl> announcements = new List<UserControl>
            {
                new Shuffle(shuffleEnabled)
            };
            App.mainPage.ShowAnnouncements(announcements, this);
        }

        /// <summary>
        /// User selects to view privacy policy in browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/aboe026/boxify/tree/master/PRIVACY.md"));
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    TvSafeArea.Toggled -= TvSafe_Toggled;
                    System.Click -= ThemeColor_Click;
                    Light.Click -= ThemeColor_Click;
                    Dark.Click -= ThemeColor_Click;
                    Spotify.Click -= Playbacksource_Click;
                    YouTube.Click -= Playbacksource_Click;
                    WelcomeConfigure.Click -= WelcomeConfigure_Click;
                    Shuffle.Click -= Shuffle_Click;
                    RateButton.Click -= Rate_Click; SpotifyGitHub.Click -= SpotifyGitHub_Click;
                    Repo.Click -= Repo_Click;
                    PrivacyButton.Click -= PrivacyButton_Click;
                });
            }
        }
    }
}
