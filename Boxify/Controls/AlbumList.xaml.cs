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
    /// Class for listing the details of an album in a row
    /// </summary>
    public sealed partial class AlbumList : UserControl
    {
        public MainPage mainPage;
        public Album Album { get; set; }

        /// <summary>
        /// The main constructor
        /// </summary>
        public AlbumList()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Constructor including references to the album whose information
        /// will be displayed as well as the MainPage from where it was created
        /// </summary>
        /// <param name="playlist">The Album whose information will be displayed</param>
        /// <param name="mainPage">The MainPage containing the Playlist</param>
        public AlbumList(Album album, MainPage mainPage) : this()
        {
            this.Album = album;
            this.mainPage = mainPage;
            DataContext = this.Album;
        }
    }
}
