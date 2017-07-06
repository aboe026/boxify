/*******************************************************************
Boxify - A Spotify client for Xbox One
Copyright(C) 2017 Adam Boe

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.If not, see<http://www.gnu.org/licenses/>.
*******************************************************************/

using Boxify.Classes;
using Boxify.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Boxify.Frames.Settings;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// A page displaying the public Spotify activity
    /// </summary>
    public sealed partial class Browse : Page
    {
        private const string FEATURED_HREF = "https://api.spotify.com/v1/browse/featured-playlists";
        private const string NEW_RELEASES_HREF = "https://api.spotify.com/v1/browse/new-releases";
        private static int featuredLimit = 6;
        private static int featuredOffset = 0;
        private static int featuredMax = 15;
        private static int newReleasesLimit = 6;
        private static int newReleasesOffset = 0;
        private static int newReleasesMax = 15;
        private static string lastTabViewed = "";

        /// <summary>
        /// The main constructor
        /// </summary>
        public Browse()
        {
            this.InitializeComponent();
            MainPage.browsePage = this;

            // Default tab
            if ((lastTabViewed == "" || lastTabViewed == "Featured"))
            {
                MainPivot.SelectedIndex = 0;
            }
            else if (lastTabViewed == "NewReleases")
            {
                MainPivot.SelectedIndex = 1;
            }
        }

        /// <summary>
        /// User switches tabs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is PivotItem)
            {
                PivotItem tab = (e.AddedItems[0] as PivotItem);
                lastTabViewed = tab.Name;
                if (tab.Name == "Featured" && FeaturedPlaylists.Items.Count == 0)
                {
                    await LoadFeaturedPlaylists();
                }
                else if (tab.Name == "NewReleases" && NewReleasesAlbums.Items.Count == 0)
                {
                    await LoadNewReleases();
                }
            }
        }

        /// <summary>
        /// Loads the featured playlists
        /// </summary>
        /// <returns></returns>
        private async Task LoadFeaturedPlaylists()
        {
            long loadingKey = DateTime.Now.Ticks;
            MainPage.AddLoadingLock(loadingKey);
            FeaturedMore.IsEnabled = false;
            FeaturedRefresh.IsEnabled = false;
            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, 0, 1, loadingKey);
            UriBuilder featuredPlaylistsBuilder = new UriBuilder(FEATURED_HREF);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", featuredLimit.ToString()),
                new KeyValuePair<string, string>("offset", featuredOffset.ToString())
            };
            featuredPlaylistsBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
            string playlistsString = await RequestHandler.SendCliGetRequest(featuredPlaylistsBuilder.Uri.ToString());
            JsonObject featuredPlaylistsJson = new JsonObject();
            try
            {
                featuredPlaylistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            if (featuredPlaylistsJson.TryGetValue("message", out IJsonValue messageJson) && messageJson.ValueType == JsonValueType.String)
            {
                FeaturedMessage.Text = messageJson.GetString();
            }
            if (featuredPlaylistsJson.TryGetValue("playlists", out IJsonValue playlistsJson) && playlistsJson.ValueType == JsonValueType.Object)
            {
                JsonObject playlists = playlistsJson.GetObject();
                if (playlists.TryGetValue("total", out IJsonValue totalJson) && totalJson.ValueType == JsonValueType.Number)
                {
                    featuredMax = Convert.ToInt32(totalJson.GetNumber());
                }
                if (playlists.TryGetValue("items", out IJsonValue itemsJson) && itemsJson.ValueType == JsonValueType.Array)
                {
                    JsonArray playlistsArray = itemsJson.GetArray();
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        if (playlistJson.GetObject().TryGetValue("href", out IJsonValue fullHref) && fullHref.ValueType == JsonValueType.String)
                        {
                            string fullPlaylistString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                            Playlist playlist = new Playlist();
                            await playlist.SetInfo(fullPlaylistString);
                            PlaylistHero playlistHero = new PlaylistHero(playlist);
                            FeaturedPlaylists.Items.Add(playlistHero);
                            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, FeaturedPlaylists.Items.Count, playlistsArray.Count, loadingKey);
                        }
                    }
                }
            }
            FeaturedRefresh.IsEnabled = true;
            if (featuredOffset + featuredLimit >= featuredMax)
            {
                FeaturedMore.Content = "No More";
                FeaturedMore.IsEnabled = false;
            }
            else
            {
                FeaturedMore.Content = "More";
                FeaturedMore.IsEnabled = true;
            }

            MainPage.RemoveLoadingLock(loadingKey);
        }

        /// <summary>
        /// Refreshes the list of featured playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void FeaturedRefresh_Click(object sender, RoutedEventArgs e)
        {
            featuredOffset = 0;
            while (FeaturedPlaylists.Items.Count > 0)
            {
                PlaylistHero playlistHero = FeaturedPlaylists.Items.ElementAt(0) as PlaylistHero;
                playlistHero.Unload();
                FeaturedPlaylists.Items.Remove(playlistHero);
            }
            await LoadFeaturedPlaylists();
        }

        /// <summary>
        /// User wishes to load more Featured Playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FeaturedMore_Click(object sender, RoutedEventArgs e)
        {
            FeaturedPlaylists.Focus(FocusState.Programmatic);
            featuredOffset += featuredLimit;
            await LoadFeaturedPlaylists();
        }

        /// <summary>
        /// User clicks a playlist to play the tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeaturedPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as PlaylistHero).playlist.PlayTracks();
        }

        /// <summary>
        /// Loads the New Releases playlists
        /// </summary>
        private async Task LoadNewReleases()
        {
            NewReleasesMore.IsEnabled = false;
            NewReleasesRefresh.IsEnabled = false;
            long loadingKey = DateTime.Now.Ticks;
            MainPage.AddLoadingLock(loadingKey);
            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, 0, 1, loadingKey);
            UriBuilder newReleasesPlaylistsBuilder = new UriBuilder(NEW_RELEASES_HREF);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", newReleasesLimit.ToString()),
                new KeyValuePair<string, string>("offset", newReleasesOffset.ToString())
            };
            newReleasesPlaylistsBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
            string playlistsString = await RequestHandler.SendCliGetRequest(newReleasesPlaylistsBuilder.Uri.ToString());
            JsonObject newReleasesPlaylistsJson = new JsonObject();
            try
            {
                newReleasesPlaylistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            if (newReleasesPlaylistsJson.TryGetValue("albums", out IJsonValue albums) && albums.ValueType == JsonValueType.Object)
            {
                JsonObject alubms = albums.GetObject();
                if (alubms.TryGetValue("total", out IJsonValue totalJson) && totalJson.ValueType == JsonValueType.Number)
                {
                    newReleasesMax = Convert.ToInt32(totalJson.GetNumber());
                }
                if (alubms.TryGetValue("items", out IJsonValue itemsJson) && itemsJson.ValueType == JsonValueType.Array)
                {
                    JsonArray albumsArray = itemsJson.GetArray();
                    foreach (JsonValue albumJson in albumsArray)
                    {
                        if (albumJson.GetObject().TryGetValue("href", out IJsonValue fullHref) && fullHref.ValueType == JsonValueType.String)
                        {
                            string fullAlbumString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                            Album alubm = new Album();
                            await alubm.SetInfo(fullAlbumString);
                            AlbumHero albumHero = new AlbumHero(alubm);
                            NewReleasesAlbums.Items.Add(albumHero);
                            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, NewReleasesAlbums.Items.Count, albumsArray.Count, loadingKey);
                        }
                    }
                }
            }
            NewReleasesRefresh.IsEnabled = true;
            if (newReleasesOffset + newReleasesLimit >= newReleasesMax)
            {
                NewReleasesMore.Content = "No More";
                NewReleasesMore.IsEnabled = false;
            }
            else
            {
                NewReleasesMore.Content = "More";
                NewReleasesMore.IsEnabled = true;
            }

            MainPage.RemoveLoadingLock(loadingKey);
        }

        /// <summary>
        /// Refreshes the list of new releases albums
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void NewReleasesRefresh_Click(object sender, RoutedEventArgs e)
        {
            newReleasesOffset = 0;
            while (NewReleasesAlbums.Items.Count > 0)
            {
                AlbumHero albumHero = NewReleasesAlbums.Items.ElementAt(0) as AlbumHero;
                albumHero.Unload();
                NewReleasesAlbums.Items.Remove(albumHero);
            }
            await LoadNewReleases();
        }

        /// <summary>
        /// User wishes to load more Featured Playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void NewReleasesMore_Click(object sender, RoutedEventArgs e)
        {
            NewReleasesAlbums.Focus(FocusState.Programmatic);
            newReleasesOffset += newReleasesLimit;
            await LoadNewReleases();
        }

        /// <summary>
        /// User clicks an album to play the tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewReleasesAlbums_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as AlbumHero).album.PlayTracks();
        }

        /// <summary>
        /// Show New Releases Tab
        /// </summary>
        public async void GoToNewReleases()
        {
            while ((MainPivot.SelectedItem as PivotItem).Name != "NewReleases")
            {
                await Task.Delay(1);
                MainPivot.SelectedIndex = 1;
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                featuredOffset = 0;
                newReleasesOffset = 0;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MainPivot.SelectionChanged -= MainPivot_SelectionChanged;

                    FeaturedPlaylists.ItemClick -= FeaturedPlaylist_ItemClick;
                    FeaturedPlaylists.ClearValue(XYFocusUpProperty);
                    while (FeaturedPlaylists.Items.Count > 0)
                    {
                        PlaylistHero playlistHero = FeaturedPlaylists.Items.ElementAt(0) as PlaylistHero;
                        FeaturedPlaylists.Items.Remove(playlistHero);
                        playlistHero.Unload();
                    }
                    FeaturedRefresh.Click -= FeaturedRefresh_Click;
                    FeaturedMore.Click -= FeaturedMore_Click;

                    NewReleasesAlbums.ItemClick -= NewReleasesAlbums_ItemClick;
                    NewReleasesAlbums.ClearValue(XYFocusUpProperty);
                    while (NewReleasesAlbums.Items.Count > 0)
                    {
                        AlbumHero albumHero = NewReleasesAlbums.Items.ElementAt(0) as AlbumHero;
                        NewReleasesAlbums.Items.Remove(albumHero);
                        albumHero.Unload();
                    }
                    NewReleasesRefresh.Click -= NewReleasesRefresh_Click;
                    NewReleasesMore.Click -= NewReleasesMore_Click;
                });
            }
        }
    }
}
