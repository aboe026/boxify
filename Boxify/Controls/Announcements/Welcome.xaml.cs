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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class Welcome : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public Welcome()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// User wishes to close the Announcements flipview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            (((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// User wishes to continue on and configure settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {

            (((((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Parent as Grid).Parent as MainPage).RightAnnouncement_Click(null, null);
        }
    }
}
