using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class YourMusic : Page
    {
        private static MainPage mainPage;
        private static string playlistsHref = "https://api.spotify.com/v1/me/playlists";
        private static List<Playlist> playlistsSave;

        public YourMusic()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }
            if (UserProfile.isLoggedIn())
            {
                warning.Visibility = Visibility.Collapsed;
                logIn.Visibility = Visibility.Collapsed;
                if (playlistsSave == null)
                {
                    await setPlaylists();
                }
                else
                {
                    foreach (Playlist playlist in playlistsSave)
                    {
                        playlists.Items.Add(playlist);
                    }
                }
            }
            else
            {
                playlistsSave = null;
                warning.Visibility = Visibility.Visible;
                logIn.Visibility = Visibility.Visible;
            }
            
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The naviagation event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            playlists.Items.Clear();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The navigation cancelled event arguments</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            playlists.Items.Clear();
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Updates the users playlists
        /// </summary>
        /// <returns></returns>
        private async Task setPlaylists()
        {
            string playlistsString = await RequestHandler.sendAuthGetRequest(playlistsHref);
            JsonObject playlistsJson = new JsonObject();
            try
            {
                playlistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue itemsJson;
            if (playlistsJson.TryGetValue("items", out itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                playlistsSave = new List<Playlist>();
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    playlist.setInfo(playlistJson.Stringify());
                    playlists.Items.Add(playlist);
                    playlistsSave.Add(playlist);
                }
            }
        }

        /// <summary>
        /// Send a user to the login page
        /// </summary>
        /// <param name="sender">The object that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void logIn_Click(object sender, RoutedEventArgs e)
        {
            mainPage.selectHamburgerOption("Profile");
        }
    }
}
