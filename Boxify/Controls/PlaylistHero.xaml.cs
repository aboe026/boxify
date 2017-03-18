using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    public sealed partial class PlaylistHero : UserControl
    {
        public MainPage mainPage;
        public Playlist Playlist { get; set; }

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
        public PlaylistHero(Playlist playlist, MainPage mainPage) : this()
        {
            this.Playlist = playlist;
            this.mainPage = mainPage;
            DataContext = this.Playlist;
        }

        /// <summary>
        /// The Play button is clicked
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void Action_Click(object sender, RoutedEventArgs e)
        {
            Playlist.PlayTracks();
        }
    }
}
