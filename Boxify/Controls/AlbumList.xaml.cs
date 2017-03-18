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
