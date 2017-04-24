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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class TvMode : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public TvMode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a predetermined toggle value
        /// </summary>
        /// <param name="enabled"></param>
        public TvMode(bool enabled) : this()
        {
            TvModeSwitch.Toggled -= TvModeSwitch_Toggled;
            TvModeSwitch.IsOn = enabled;
            TvModeSwitch.Toggled += TvModeSwitch_Toggled;
        }

        /// <summary>
        /// User wishes to change the TV mode setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TvModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            bool enabled = (sender as ToggleSwitch).IsOn;
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.SetTvSafeUI(enabled);
            }
            else
            {
                Settings.SetTvSafe(enabled);
                if (enabled)
                {
                    App.mainPage.SafeAreaOn();
                }
                else
                {
                    App.mainPage.SafeAreaOff();
                }
            }
        }
    }
}
