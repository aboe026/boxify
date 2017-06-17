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

using Boxify.Frames;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Boxify.Frames.Settings;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaybackMode : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public PlaybackMode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a pre-defined source
        /// </summary>
        /// <param name="source">The current source of the app</param>
        public PlaybackMode(Playbacksource source) : this()
        {
            YouTube.Click -= PlaybackMode_Click;
            Spotify.Click -= PlaybackMode_Click;
            if (source == Playbacksource.Spotify)
            {
                Spotify.IsChecked = true;
            }
            else if (source == Playbacksource.YouTube)
            {
                YouTube.IsChecked = true;
            }
            YouTube.Click += PlaybackMode_Click;
            Spotify.Click += PlaybackMode_Click;
        }

        /// <summary>
        /// User clicks to change the playback source
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaybackMode_Click(object sender, RoutedEventArgs e)
        {
            Playbacksource source = Playbacksource.Spotify;
            if (Spotify.IsChecked == true)
            {
                source = Playbacksource.Spotify;
            }
            else if (YouTube.IsChecked == true)
            {
                source = Playbacksource.YouTube;
            }
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.SetPlaybackSourceUI(source);
            }
            else
            {
                SetPlaybackSource(source);
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (MainPage.closedAnnouncements || (App.isInBackgroundMode && MainPage.closedAnnouncements))
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Spotify.Click -= PlaybackMode_Click;
                    YouTube.Click -= PlaybackMode_Click;
                });
            }
        }
    }
}
