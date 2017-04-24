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

using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    /// <summary>
    /// A class for listing the details of a playlist in a row
    /// </summary>
    public sealed partial class PlaylistList : UserControl
    {
        public Playlist Playlist { get; set; }

        /// <summary>
        /// The main constructor
        /// </summary>
        public PlaylistList()
        {
            this.InitializeComponent();
            action.Foreground = new SolidColorBrush(Colors.Transparent);
        }
        
        /// <summary>
        /// Constructor including references to the playlist whose information
        /// will be displayed as well as the MainPage from where it was created
        /// </summary>
        /// <param name="playlist">The Playlist whose information will be displayed</param>
        /// <param name="mainPage">The MainPage containing the Playlist</param>
        public PlaylistList(Playlist playlist) : this()
        {
            this.Playlist = playlist;
            DataContext = this.Playlist;
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Unload()
        {
            Bindings.StopTracking();
            Playlist = null;
        }
    }
}
