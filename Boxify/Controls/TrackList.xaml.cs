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

using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Frames
{
    /// <summary>
    /// Class for listing the details of a track in a row
    /// </summary>
    public sealed partial class TrackList : UserControl
    {
        public Track track;

        /// <summary>
        /// The main constructor
        /// </summary>
        public TrackList()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Constructor including references to the track whose information
        /// will be displayed as well as the MainPage from where it was created
        /// </summary>
        /// <param name="playlist">The Track whose information will be displayed</param>
        /// <param name="mainPage">The MainPage containing the Playlist</param>
        public TrackList(Track track) : this()
        {
            this.track = track;
            PopulateData();
        }

        /// <summary>
        /// Populate UI with Track information
        /// </summary>
        private void PopulateData()
        {
            Image.Source = track.album.image;
            Name.Text = track.name;
            Artist.Text = track.GetMainArtistName();
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Unload()
        {
            track = null;

            Image.ClearValue(Image.SourceProperty);
            Name.ClearValue(TextBlock.TextProperty);
            ArtistLabel.ClearValue(TextBlock.TextProperty);
            Artist.ClearValue(TextBlock.TextProperty);

            Image = null;
            Name = null;
            ArtistLabel = null;
            Artist = null;
        }
    }
}
