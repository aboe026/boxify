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
            loading.IsActive = true;
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
                    featuredPlaylistsSave = new List<PlaylistList>();
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        Playlist playlist = new Playlist();
                        await playlist.setInfo(playlistJson.Stringify());
                        PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                        featuredPlaylists.Items.Add(playlistList);
                        featuredPlaylistsSave.Add(playlistList);
                    }
                }
            }
            loading.IsActive = false;
            refresh.Visibility = Visibility.Visible;
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
    }
}
