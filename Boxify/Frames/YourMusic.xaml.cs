using System;
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
        private static int playlistLimit = 10;
        private static int playlistsOffset = 0;
        private static int playlistsTotal = 20;
        public static List<PlaylistList> playlistsSave;
        public static bool refreshing = false;
        private static int playlistsCount = 0;

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
                More.IsEnabled = false;
                warning.Visibility = Visibility.Collapsed;
                logIn.Visibility = Visibility.Collapsed;
                if (playlistsSave == null)
                {
                    await LoadPlaylists();
                }
                else
                {
                    if (refreshing)
                    {
                        refresh.Visibility = Visibility.Collapsed;
                        mainPage.setSpotifyLoadingMaximum(playlistsCount);
                        mainPage.setSpotifyLoadingValue(0);
                        mainPage.bringUpSpotify();
                        while (refreshing)
                        {
                            mainPage.setSpotifyLoadingMaximum(playlistsCount);
                            mainPage.setSpotifyLoadingValue(playlistsSave.Count);
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                        }
                    }
                    foreach (PlaylistList playlist in playlistsSave)
                    {
                        try
                        {
                            playlists.Items.Add(playlist);
                        }
                        catch (COMException) { }
                    }
                    refresh.Visibility = Visibility.Visible;
                }

                if (playlistsOffset + playlistLimit >= playlistsTotal)
                {
                    More.Content = "No More";
                    More.IsEnabled = false;
                }
                else
                {
                    More.Content = "More...";
                    More.IsEnabled = true;
                }
            }
            else
            {
                More.Visibility = Visibility.Collapsed;
                playlistsSave = null;
                playlistsLabel.Visibility = Visibility.Collapsed;
                refresh.Visibility = Visibility.Collapsed;
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
        /// Refreshes the users playlists
        /// </summary>
        /// <returns></returns>
        private async Task LoadPlaylists()
        {
            More.IsEnabled = false;
            refresh.IsEnabled = false;
            mainPage.setSpotifyLoadingValue(0);
            mainPage.bringUpSpotify();

            UriBuilder playlistsBuilder = new UriBuilder(playlistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("limit", playlistLimit.ToString()));
            queryParams.Add(new KeyValuePair<string, string>("offset", playlistsOffset.ToString()));
            playlistsBuilder.Query = RequestHandler.convertToQueryString(queryParams);
            string playlistsString = await RequestHandler.sendAuthGetRequest(playlistsBuilder.Uri.ToString());
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
            IJsonValue totalJson;
            if (playlistsJson.TryGetValue("total", out totalJson))
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                mainPage.setSpotifyLoadingMaximum(playlistsArray.Count);
                if (playlistsSave == null)
                {
                    playlistsSave = new List<PlaylistList>();
                }
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.setInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                    playlistsSave.Add(playlistList);
                    playlists.Items.Add(playlistList);
                    mainPage.setSpotifyLoadingValue(playlistsSave.Count);
                }
            }
            refresh.IsEnabled = true;
            if (playlistsOffset + playlistLimit >= playlistsTotal)
            {
                More.Content = "No More";
                More.IsEnabled = false;
            }
            else
            {
                More.Content = "More...";
                More.IsEnabled = true;
            }
        }

        /// <summary>
        /// Sets the playlists without updating the UI
        /// </summary>
        /// <returns></returns>
        public static async Task setPlaylists()
        {
            UriBuilder playlistsBuilder = new UriBuilder(playlistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("limit", playlistLimit.ToString()));
            queryParams.Add(new KeyValuePair<string, string>("offset", playlistsOffset.ToString()));
            playlistsBuilder.Query = RequestHandler.convertToQueryString(queryParams);
            string playlistsString = await RequestHandler.sendAuthGetRequest(playlistsBuilder.Uri.ToString());
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
            IJsonValue totalJson;
            if (playlistsJson.TryGetValue("total", out totalJson))
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                playlistsCount = playlistsArray.Count;
                if (playlistsSave == null)
                {
                    playlistsSave = new List<PlaylistList>();
                }
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.setInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                    playlistsSave.Add(playlistList);
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
            mainPage.selectHamburgerOption("ProfileItem");
        }

        /// <summary>
        /// Refresh users playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void refresh_Click(object sender, RoutedEventArgs e)
        {
            playlistsOffset = 0;
            playlistsSave = new List<PlaylistList>();
            playlists.Items.Clear();
            await LoadPlaylists();
        }

        /// <summary>
        /// When user clicks a PlaylistList for playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void playlists_ItemClick(object sender, ItemClickEventArgs e)
        {
            await (e.ClickedItem as PlaylistList).playlist.playTracks();
        }

        /// <summary>
        /// User wishes to load more of their playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            playlists.Focus(FocusState.Programmatic);
            playlistsOffset += playlistLimit;
            await LoadPlaylists();
        }

        /// <summary>
        /// Used when freeing memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                playlistsOffset = 0;
                playlistsSave = null;
            }
        }
    }
}
