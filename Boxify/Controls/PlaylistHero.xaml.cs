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
    public sealed partial class PlaylistHero : UserControl
    {
        public Playlist playlist;

        /// <summary>
        /// The main constructor
        /// </summary>
        public PlaylistHero()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Constructor including references to the playlist whose information
        /// will be displayed as well as the MainPage from where it was created
        /// </summary>
        /// <param name="playlist">The Playlist whose information will be displayed</param>
        /// <param name="mainPage">The MainPage containing the Playlist</param>
        public PlaylistHero(Playlist playlist) : this()
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
            Description.Text = playlist.description;
            Tracks.Text = playlist.tracksCount.ToString();
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Unload()
        {
            playlist = null;

            Image = null;
            DisplayName = null;
            Description = null;
            TracksLabel = null;
            Tracks = null;
        }
    }
}
