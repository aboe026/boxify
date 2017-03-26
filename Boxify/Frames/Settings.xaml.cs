﻿/*******************************************************************
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

        public static MainPage mainPage;

        public static bool tvSafeArea = true;
        public static Theme theme = Theme.System;
        public static Playbacksource playbackSource = Playbacksource.Spotify;
        public static bool repeatEnabled = false;
        public static double volume = 100;
        public static string version = "";

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
                    mainPage.SafeAreaOn();
                }
                else
                {
                    mainPage.SafeAreaOff();
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
            tvSafeArea = enabled;
            TvSafeArea.IsOn = enabled;
            if (enabled)
            {
                if (mainPage != null)
                {
                    mainPage.SafeAreaOn();
                }
            }
            else
            {
                if (mainPage != null)
                {
                    mainPage.SafeAreaOff();
                }
            }
            SaveSettings();
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
                    mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
            else if ((sender as RadioButton).Name == "Light")
            {
                theme = Theme.Light;
                mainPage.RequestedTheme = ElementTheme.Light;
            }
            else if ((sender as RadioButton).Name == "Dark")
            {
                theme = Theme.Dark;
                mainPage.RequestedTheme = ElementTheme.Dark;
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the theme setting and adjust the UI
        /// </summary>
        /// <param name="newTheme">The theme to set the UI to</param>
        public void SetThemeUI(Theme newTheme)
        {
            theme = newTheme;
            if (theme == Theme.System && mainPage != null)
            {
                System.IsChecked = true;
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
            else if (theme == Theme.Light && mainPage != null)
            {
                Light.IsChecked = true;
                mainPage.RequestedTheme = ElementTheme.Light;
            }
            else if (theme == Theme.Dark && mainPage != null)
            {
                Dark.IsChecked = true;
                mainPage.RequestedTheme = ElementTheme.Dark;
            }
            SaveSettings();
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
                playbackSource = Playbacksource.Spotify;
            }
            else if (((RadioButton)sender).Name == "YouTube")
            {
                playbackSource = Playbacksource.YouTube;
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the playback type and update the UI
        /// </summary>
        /// <param name="source">The source to get tracks from</param>
        public void SetPlaybackSourceUI(Playbacksource source)
        {
            playbackSource = source;
            if (source == Playbacksource.Spotify)
            {
                Spotify.IsChecked = true;
            }
            else if (source == Playbacksource.YouTube)
            {
                YouTube.IsChecked = true;
            }
            SaveSettings();
        }

        /// <summary>
        /// Change the playback type
        /// </summary>
        /// <param name="source"></param>
        public static void SetPlaybackSource(Playbacksource source)
        {
            playbackSource = source;
            SaveSettings();
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
        /// Show announcements for the 1.0.0.0 version
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Announcement1000_Click(object sender, RoutedEventArgs e)
        {
            List<UserControl> announcements = new List<UserControl>
            {
                new Welcome(),
                new TvMode(tvSafeArea),
                new ThemeMode(theme),
                new PlaybackMode(playbackSource)
            };
            mainPage.ShowAnnouncements(announcements, this);
        }
    }
}
