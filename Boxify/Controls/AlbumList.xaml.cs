using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Frames
{
    /// <summary>
    /// Class for listing the details of an album in a row
    /// </summary>
    public sealed partial class AlbumList : UserControl
    {
        public MainPage mainPage;
        public Album album { get; set; }

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
            this.album = album;
            this.mainPage = mainPage;
            DataContext = this.album;
        }

        /// <summary>
        /// The Play button is clicked
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private async void action_Click(object sender, RoutedEventArgs e)
        {
            List<Track> tracks = await album.getTracks();
            PlaybackService.playQueue(tracks);
        }
    }
}
