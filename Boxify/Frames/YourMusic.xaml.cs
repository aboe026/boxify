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
            LoadingProgress.Visibility = Visibility.Collapsed;
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
                    await refreshPlaylists();
                }
                else
                {
                    refresh.Visibility = Visibility.Collapsed;
                    LoadingProgress.Maximum = playlistsCount;
                    LoadingProgress.Value = 0;
                    LoadingProgress.Visibility = Visibility.Visible;
                    while (refreshing)
                    {
                        LoadingProgress.Maximum = playlistsCount;
                        LoadingProgress.Value = playlistsSave.Count;
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
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
                    LoadingProgress.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                playlistsSave = null;
                playlistsLabel.Visibility = Visibility.Collapsed;
                refresh.Visibility = Visibility.Collapsed;
                LoadingProgress.Visibility = Visibility.Collapsed;
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
        private async Task refreshPlaylists()
        {
            refresh.Visibility = Visibility.Collapsed;
            LoadingProgress.Value = 0;
            LoadingProgress.Visibility = Visibility.Visible;
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
                LoadingProgress.Maximum = playlistsArray.Count;
                playlistsSave = new List<PlaylistList>();
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.setInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                    playlistsSave.Add(playlistList);
                    playlists.Items.Add(playlistList);
                    LoadingProgress.Value = playlistsSave.Count;
                }
            }
            refresh.Visibility = Visibility.Visible;
            LoadingProgress.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Sets the playlists without updating the UI
        /// </summary>
        /// <returns></returns>
        public static async Task setPlaylists()
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
                playlistsCount = playlistsArray.Count;
                playlistsSave = new List<PlaylistList>();
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
            playlists.Items.Clear();
            await refreshPlaylists();
        }

        /// <summary>
        /// When user hovers onto PlaylistList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playlists_GotFocus(object sender, RoutedEventArgs e)
        {
            ListViewItem item = e.OriginalSource as ListViewItem;
            playlists.SelectedIndex = getListIndex(item);
            (item.Content as PlaylistList).showPlay();
        }

        /// <summary>
        /// When user hovers away from PlaylistList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playlists_LostFocus(object sender, RoutedEventArgs e)
        {
            ((e.OriginalSource as ListViewItem).Content as PlaylistList).hidePlay();
        }

        /// <summary>
        /// Gets the index of the currently hovered PlaylistList
        /// </summary>
        /// <param name="item">The item currently hovered</param>
        /// <returns>The index of the currently hovered item in the ListView</returns>
        private int getListIndex(ListViewItem item)
        {
            for (int i = 0; i < playlists.Items.Count; i++)
            {
                if ((playlists.Items[i] as PlaylistList).playlist.id == (item.Content as PlaylistList).playlist.id)
                {
                    return i;
                }
            }
            return 0;
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
    }
}
