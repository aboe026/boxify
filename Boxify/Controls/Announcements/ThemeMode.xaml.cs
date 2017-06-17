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
    public sealed partial class ThemeMode : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public ThemeMode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a pre-defined theme
        /// </summary>
        /// <param name="theme">The current theme of the app</param>
        public ThemeMode(Theme theme) : this()
        {
            System.Click -= ThemeMode_Click;
            Light.Click -= ThemeMode_Click;
            Dark.Click -= ThemeMode_Click;
            if (theme == Theme.System)
            {
                System.IsChecked = true;
            }
            else if (theme == Theme.Light)
            {
                Light.IsChecked = true;
            }
            else if (theme == Theme.Dark)
            {
                Dark.IsChecked = true;
            }
            System.Click += ThemeMode_Click;
            Light.Click += ThemeMode_Click;
            Dark.Click += ThemeMode_Click;
        }

        /// <summary>
        /// User selects to change the theme mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeMode_Click(object sender, RoutedEventArgs e)
        {
            Theme newTheme = Theme.System;
            if ((sender as RadioButton).Name == "System")
            {
                newTheme = Theme.System;
            }
            else if ((sender as RadioButton).Name == "Light")
            {
                newTheme = Theme.Light;
            }
            else if ((sender as RadioButton).Name == "Dark")
            {
                newTheme = Theme.Dark;
            }

            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.SetThemeUI(newTheme);
            }
            else
            {
                Settings.SetTheme(newTheme);
                if (newTheme == Theme.System)
                {
                    if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                    {
                        App.mainPage.RequestedTheme = ElementTheme.Light;
                    }
                    else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    {
                        App.mainPage.RequestedTheme = ElementTheme.Dark;
                    }
                }
                else if (newTheme == Theme.Light)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (newTheme == Theme.Dark)
                {
                    App.mainPage.RequestedTheme = ElementTheme.Dark;
                }
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
                    System.Click -= ThemeMode_Click;
                    Light.Click -= ThemeMode_Click;
                    Dark.Click -= ThemeMode_Click;
                });
            }
        }
    }
}
