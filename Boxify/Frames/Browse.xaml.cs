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
    /// A page displaying the public Spotify activity
    /// </summary>
    public sealed partial class Browse : Page
    {
        private static MainPage mainPage;
        private static string featuredPlaylistsHref = "https://api.spotify.com/v1/browse/featured-playlists";
        private static int featuredPlaylistLimit = 5;
        private static string featuredPlaylistsMessageSave = "";
        private static List<PlaylistList> featuredPlaylistsSave;

        /// <summary>
        /// The main constructor
        /// </summary>
        public Browse()
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
            if (featuredPlaylistsSave == null)
            {
                await setFeaturedPlaylists();
            }
            else
            {
                featuredPlaylistMessage.Text = featuredPlaylistsMessageSave;
                foreach (PlaylistList playlist in featuredPlaylistsSave)
                {
                    featuredPlaylists.Items.Add(playlist);
                }
            }
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The naviagation event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            featuredPlaylists.Items.Clear();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The navigation cancelled event arguments</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            featuredPlaylists.Items.Clear();
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Updates the featured playlists
        /// </summary>
        /// <returns></returns>
        private async Task setFeaturedPlaylists()
        {
            refresh.Visibility = Visibility.Collapsed;
            LoadingProgress.Value = 0;
            LoadingProgress.Visibility = Visibility.Visible;
            UriBuilder featuredPlaylistsBuilder = new UriBuilder(featuredPlaylistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("limit", featuredPlaylistLimit.ToString()));
            string queryParamsString = RequestHandler.convertToQueryString(queryParams);
            featuredPlaylistsBuilder.Query = queryParamsString;
            string playlistsString = await RequestHandler.sendCliGetRequest(featuredPlaylistsBuilder.Uri.ToString());
            JsonObject featuredPlaylistsJson = new JsonObject();
            try
            {
                featuredPlaylistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue messageJson;
            IJsonValue playlistsJson;
            IJsonValue itemsJson;
            if (featuredPlaylistsJson.TryGetValue("message", out messageJson))
            {
                featuredPlaylistMessage.Text = messageJson.GetString();
                featuredPlaylistsMessageSave = featuredPlaylistMessage.Text;
            }
            if (featuredPlaylistsJson.TryGetValue("playlists", out playlistsJson))
            {
                JsonObject playlists = playlistsJson.GetObject();
                if (playlists.TryGetValue("items", out itemsJson))
                {
                    JsonArray playlistsArray = itemsJson.GetArray();
                    LoadingProgress.Maximum = playlistsArray.Count;
                    featuredPlaylistsSave = new List<PlaylistList>();
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        Playlist playlist = new Playlist();
                        await playlist.setInfo(playlistJson.Stringify());
                        PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                        featuredPlaylists.Items.Add(playlistList);
                        featuredPlaylistsSave.Add(playlistList);
                        LoadingProgress.Value = featuredPlaylistsSave.Count;
                    }
                }
            }
            refresh.Visibility = Visibility.Visible;
            LoadingProgress.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Refreshes the list of featured playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void refresh_Click(object sender, RoutedEventArgs e)
        {
            featuredPlaylists.Items.Clear();
            await setFeaturedPlaylists();
        }

        /// <summary>
        /// When user hovers onto PlaylistList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void featuredPlaylists_GotFocus(object sender, RoutedEventArgs e)
        {
            ListViewItem item = e.OriginalSource as ListViewItem;
            featuredPlaylists.SelectedIndex = getListIndex(item);
            (item.Content as PlaylistList).showPlay();
        }

        /// <summary>
        /// When User hovers away from PlaylistList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void featuredPlaylists_LostFocus(object sender, RoutedEventArgs e)
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
            for (int i=0; i < featuredPlaylists.Items.Count; i++)
            {
                if ((featuredPlaylists.Items[i] as PlaylistList).playlist.id == (item.Content as PlaylistList).playlist.id)
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// PlaylistList is clicked for playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void featuredPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            await (e.ClickedItem as PlaylistList).playlist.playTracks();
        }
    }
}
