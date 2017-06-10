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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class PlaybackOptions : UserControl
    {
        public PlaybackOptions()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a pre-defined source
        /// </summary>
        /// <param name="repeatEnabled">Whether repeat is currently enabled or not</param>
        /// <param name="shuffleEnabled">Whether shuffle is currently enabled or not</param>
        public PlaybackOptions(bool repeatEnabled, bool shuffleEnabled) : this()
        {
            RepeatSwitch.Toggled -= RepeatSwitch_Toggled;
            ShuffleSwitch.Toggled -= ShuffleSwitch_Toggled;
            RepeatSwitch.IsOn = repeatEnabled;
            ShuffleSwitch.IsOn = shuffleEnabled;
            RepeatSwitch.Toggled += RepeatSwitch_Toggled;
            ShuffleSwitch.Toggled += ShuffleSwitch_Toggled;
        }

        /// <summary>
        /// User chooses to toggle on or off playback repeat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepeatSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.ToggleRepeat();
            }
            else
            {
                Settings.SetRepeat(RepeatSwitch.IsOn);
            }
        }

        /// <summary>
        /// User chooses to toggle on or off playback shuffle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShuffleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.ToggleShuffle();
            }
            else
            {
                Settings.SetShuffle(ShuffleSwitch.IsOn);
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (RepeatSwitch != null)
                {
                    RepeatSwitch.Toggled -= RepeatSwitch_Toggled;
                    RepeatSwitch = null;
                }
                if (ShuffleSwitch != null)
                {
                    ShuffleSwitch.Toggled -= ShuffleSwitch_Toggled;
                    ShuffleSwitch = null;
                }

                RepeatLabel = null;
                ShuffleLabel = null;
                ToggleGrid = null;

                Message = null;
                Header = null;

                CenteredPanel = null;
            });
        }
    }
}
