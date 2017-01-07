using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Frames
{
    /// <summary>
    /// Class for listing the details of a track in a row
    /// </summary>
    public sealed partial class TrackList : UserControl
    {
        public MainPage mainPage;
        public Track track { get; set; }

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
        public TrackList(Track track, MainPage mainPage) : this()
        {
            this.track = track;
            this.mainPage = mainPage;
            DataContext = this.track;
        }

        /// <summary>
        /// The Play button is clicked
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void action_Click(object sender, RoutedEventArgs e)
        {
            List<Track> tracks = new List<Track>();
            tracks.Add(track);
            PlaybackService.playQueue(tracks);
        }
    }
}
