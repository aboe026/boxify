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

using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls
{
    /// <summary>
    /// A class for listing the details of a playlist in a row
    /// </summary>
    public sealed partial class PlaylistList : UserControl
    {
        public Playlist playlist;

        /// <summary>
        /// The main constructor
        /// </summary>
        public PlaylistList()
        {
            this.InitializeComponent();
        }
        
        /// <summary>
        /// Constructor including references to the playlist whose information
        /// will be displayed as well as the MainPage from where it was created
        /// </summary>
        /// <param name="playlist">The Playlist whose information will be displayed</param>
        /// <param name="mainPage">The MainPage containing the Playlist</param>
        public PlaylistList(Playlist playlist) : this()
        {
            this.playlist = playlist;
            PopulateData();
        }

        /// <summary>
        /// Populate UI with Playlist information
        /// </summary>
        public void PopulateData()
        {
            Image.Source = playlist.image;
            DisplayName.Text = playlist.name;
            Tracks.Text = playlist.tracksCount.ToString();
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Unload()
        {
            playlist.Dispose();
            playlist = null;

            Image.Source = null;
            Image = null;
            DisplayName = null;
            Tracks = null;
            TracksLabel = null;
        }
    }
}
