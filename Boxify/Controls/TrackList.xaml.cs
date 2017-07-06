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
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls
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
            DisplayName.Text = track.name;
            Artist.Text = track.GetMainArtistName();
            Album.Text = track.album.GetMainArtistName();
            TimeSpan duration = TimeSpan.FromSeconds(Convert.ToDouble(track.duration) / 1000);
            if (duration.TotalHours < 1)
            {
                Duration.Text = (duration).ToString(@"mm\:ss");
            }
            else
            {
                Duration.Text = (Math.Floor(duration.TotalHours)).ToString() + ":" + (duration).ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// Sets light background for odd/even row distinguishability
        /// </summary>
        public void TurnOffOpaqueBackground()
        {
            Background.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Unload()
        {
            track.Dispose();

            Image.ClearValue(Image.SourceProperty);
        }
    }
}
