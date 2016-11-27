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
    /// The page displaying the users custom music selections
    /// </summary>
    public sealed partial class YourMusic : Page
    {
        private static MainPage mainPage;
        private static string playlistsHref = "https://api.spotify.com/v1/me/playlists";
        private static List<PlaylistList> playlistsSave;

        /// <summary>
        /// The main constructor
        /// </summary>
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
                    foreach (PlaylistList playlist in playlistsSave)
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
            refresh.Visibility = Visibility.Collapsed;
            loading.IsActive = true;
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
                playlistsSave = new List<PlaylistList>();
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.setInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                    playlists.Items.Add(playlistList);
                    playlistsSave.Add(playlistList);
                }
            }
            loading.IsActive = false;
            refresh.Visibility = Visibility.Visible;
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

        /// <summary>
        /// Refresh users playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void refresh_Click(object sender, RoutedEventArgs e)
        {
            playlists.Items.Clear();
            await setPlaylists();
        }
    }
}
