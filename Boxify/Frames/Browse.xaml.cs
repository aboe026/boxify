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
        private static int featuredPlaylistLimit = 6;
        private static int featuredPlaylistsOffset = 0;
        private static int featuredPlaylistsTotal = 15;
        private static string featuredPlaylistsMessageSave = "";
        private static List<PlaylistHero> featuredPlaylistsSave;

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
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }
            if (featuredPlaylistsSave == null)
            {
                await LoadFeaturedPlaylists();
            }
            else
            {
                featuredPlaylistMessage.Text = featuredPlaylistsMessageSave;
                foreach (PlaylistHero playlist in featuredPlaylistsSave)
                {
                    try
                    {
                        FeaturedPlaylists.Items.Add(playlist);
                    }
                    catch (COMException) { }
                }
            }
            if (featuredPlaylistsOffset + featuredPlaylistLimit >= featuredPlaylistsTotal)
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
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The naviagation event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FeaturedPlaylists.Items.Clear();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The navigation cancelled event arguments</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            FeaturedPlaylists.Items.Clear();
            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Loads the featured playlists
        /// </summary>
        /// <returns></returns>
        private async Task LoadFeaturedPlaylists()
        {
            More.IsEnabled = false;
            refresh.IsEnabled = false;
            mainPage.setSpotifyLoadingValue(0);
            mainPage.bringUpSpotify();
            UriBuilder featuredPlaylistsBuilder = new UriBuilder(featuredPlaylistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("limit", featuredPlaylistLimit.ToString()));
            queryParams.Add(new KeyValuePair<string, string>("offset", featuredPlaylistsOffset.ToString()));
            featuredPlaylistsBuilder.Query = RequestHandler.convertToQueryString(queryParams);
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
            IJsonValue totalJson;
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
                if (playlists.TryGetValue("total", out totalJson))
                {
                    featuredPlaylistsTotal = Convert.ToInt32(totalJson.GetNumber());
                }
                if (playlists.TryGetValue("items", out itemsJson))
                {
                    JsonArray playlistsArray = itemsJson.GetArray();
                    mainPage.setSpotifyLoadingMaximum(playlistsArray.Count);
                    if (featuredPlaylistsSave == null)
                    {
                        featuredPlaylistsSave = new List<PlaylistHero>();
                    }
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        IJsonValue fullHref;
                        if (playlistJson.GetObject().TryGetValue("href", out fullHref)) {
                            string fullPlaylistString = await RequestHandler.sendCliGetRequest(fullHref.GetString());
                            Playlist playlist = new Playlist();
                            await playlist.setInfo(fullPlaylistString);
                            PlaylistHero playlistHero = new PlaylistHero(playlist, mainPage);
                            FeaturedPlaylists.Items.Add(playlistHero);
                            featuredPlaylistsSave.Add(playlistHero);
                            mainPage.setSpotifyLoadingValue(featuredPlaylistsSave.Count);
                        }
                    }
                }
            }
            refresh.IsEnabled = true;
            if (featuredPlaylistsOffset + featuredPlaylistLimit >= featuredPlaylistsTotal)
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
        /// Refreshes the list of featured playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void refresh_Click(object sender, RoutedEventArgs e)
        {
            featuredPlaylistsOffset = 0;
            featuredPlaylistsSave = new List<PlaylistHero>();
            FeaturedPlaylists.Items.Clear();
            await LoadFeaturedPlaylists();
        }

        /// <summary>
        /// When user hovers onto PlaylistList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeaturedPlaylists_GotFocus(object sender, RoutedEventArgs e)
        {
                GridViewItem item = e.OriginalSource as GridViewItem;
                FeaturedPlaylists.SelectedIndex = getListIndex(item);
        }

        /// <summary>
        /// Gets the index of the currently hovered PlaylistList
        /// </summary>
        /// <param name="item">The item currently hovered</param>
        /// <returns>The index of the currently hovered item in the ListView</returns>
        private int getListIndex(GridViewItem item)
        {
            for (int i=0; i < FeaturedPlaylists.Items.Count; i++)
            {
                if ((FeaturedPlaylists.Items[i] as PlaylistHero).playlist.id == (item.Content as PlaylistHero).playlist.id)
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// User clicks a playlist to play the tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FeaturedPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            await (e.ClickedItem as PlaylistHero).playlist.playTracks();
        }

        /// <summary>
        /// User wishes to load more Featured Playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            FeaturedPlaylists.Focus(FocusState.Programmatic);
            featuredPlaylistsOffset += featuredPlaylistLimit;
            await LoadFeaturedPlaylists();
        }
    }
}
