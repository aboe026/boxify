using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Boxify.Playlist;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    public sealed partial class PlaylistList : UserControl
    {
        public MainPage mainPage;
        public Playlist playlist { get; set; }

        public PlaylistList()
        {
            this.InitializeComponent();
        }
        
        public PlaylistList(Playlist playlist, MainPage mainPage) : this()
        {
            this.playlist = playlist;
            this.mainPage = mainPage;
            DataContext = this.playlist;
        }

        /// <summary>
        /// The Play/Pause button is clicked
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private async void action_Click(object sender, RoutedEventArgs e)
        {
            Button actionButton = (Button)sender;
            if (playlist.state == State.Paused)    // play button
            {
                actionButton.Content = "\uE769";
                actionButton.Foreground = new SolidColorBrush(Colors.Black);
                playlist.state = State.Playing;
                List<Track> tracks = await playlist.getTracks();
                mainPage.setQueue(tracks);
            }
            else if (playlist.state == State.Playing)
            {
                actionButton.Content = "\uE768";
                actionButton.Foreground = new SolidColorBrush(Colors.Green);
                playlist.state = State.Paused;
            }
        }
    }
}
