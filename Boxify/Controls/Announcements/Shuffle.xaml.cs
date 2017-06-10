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

using System;
using Boxify.Frames;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class Shuffle : UserControl
    {
        public Shuffle()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a pre-defined source
        /// </summary>
        /// <param name="enabled">Whether or not shuffle is currently enabled</param>
        public Shuffle(bool enabled) : this()
        {
            if (enabled)
            {
                ShuffleIcon.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ShuffleIcon.Foreground = (SolidColorBrush)Resources["PlaybackButtonForeground"];
            }
            ShuffleSwitch.Toggled -= ShuffleSwitch_Toggled;
            ShuffleSwitch.IsOn = enabled;
            ShuffleSwitch.Toggled += ShuffleSwitch_Toggled;
        }

        /// <summary>
        /// User wishes to switch shuffle on or off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShuffleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (ShuffleSwitch.IsOn)
            {
                ShuffleIcon.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ShuffleIcon.Foreground = (SolidColorBrush)Resources["PlaybackButtonForeground"];
            }
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.ToggleShuffle();
            }
            else
            {
                Settings.SetShuffle(!Settings.shuffleEnabled);
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (MainPage.closedAnnouncements || App.isInBackgroundMode)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (ShuffleSwitch != null)
                    {
                        ShuffleSwitch.Toggled -= ShuffleSwitch_Toggled;
                        ShuffleSwitch = null;
                    }

                    ShuffleIcon = null;
                    Message = null;
                    Header = null;
                    Version = null;

                    CenteredPanel = null;
                });
            }
        }
    }
}
