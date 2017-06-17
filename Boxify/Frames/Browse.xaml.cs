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
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// A page displaying the public Spotify activity
    /// </summary>
    public sealed partial class Browse : Page
    {
        private static string featuredPlaylistsHref = "https://api.spotify.com/v1/browse/featured-playlists";
        private static int featuredPlaylistLimit = 6;
        private static int featuredPlaylistsOffset = 0;
        private static int featuredPlaylistsTotal = 15;

        /// <summary>
        /// The main constructor
        /// </summary>
        public Browse()
        {
            this.InitializeComponent();
            MainPage.browsePage = this;
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (FeaturedPlaylists.Items.Count == 0)
            {
                await LoadFeaturedPlaylists();
            }
        }

        /// <summary>
        /// Loads the featured playlists
        /// </summary>
        /// <returns></returns>
        private async Task LoadFeaturedPlaylists()
        {
            More.IsEnabled = false;
            Refresh.IsEnabled = false;
            App.mainPage.SetSpotifyLoadingValue(0);
            App.mainPage.BringUpSpotify();
            UriBuilder featuredPlaylistsBuilder = new UriBuilder(featuredPlaylistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", featuredPlaylistLimit.ToString()),
                new KeyValuePair<string, string>("offset", featuredPlaylistsOffset.ToString())
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
            if (featuredPlaylistsJson.TryGetValue("message", out IJsonValue messageJson))
            {
                FeaturedPlaylistMessage.Text = messageJson.GetString();
            }
            if (featuredPlaylistsJson.TryGetValue("playlists", out IJsonValue playlistsJson))
            {
                JsonObject playlists = playlistsJson.GetObject();
                if (playlists.TryGetValue("total", out IJsonValue totalJson))
                {
                    featuredPlaylistsTotal = Convert.ToInt32(totalJson.GetNumber());
                }
                if (playlists.TryGetValue("items", out IJsonValue itemsJson))
                {
                    JsonArray playlistsArray = itemsJson.GetArray();
                    App.mainPage.SetSpotifyLoadingMaximum(playlistsArray.Count);
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        if (playlistJson.GetObject().TryGetValue("href", out IJsonValue fullHref))
                        {
                            string fullPlaylistString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                            Playlist playlist = new Playlist();
                            await playlist.SetInfo(fullPlaylistString);
                            PlaylistHero playlistHero = new PlaylistHero(playlist);
                            FeaturedPlaylists.Items.Add(playlistHero);
                            App.mainPage.SetSpotifyLoadingValue(FeaturedPlaylists.Items.Count);
                        }
                    }
                }
            }
            Refresh.IsEnabled = true;
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
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            featuredPlaylistsOffset = 0;
            while (FeaturedPlaylists.Items.Count > 0)
            {
                PlaylistHero playlistHero = FeaturedPlaylists.Items.ElementAt(0) as PlaylistHero;
                playlistHero.Unload();
                FeaturedPlaylists.Items.Remove(playlistHero);
            }
            await LoadFeaturedPlaylists();
        }

        /// <summary>
        /// User clicks a playlist to play the tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeaturedPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as PlaylistHero).playlist.PlayTracks();
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

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                featuredPlaylistsOffset = 0;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Bindings.StopTracking();

                    FeaturedPlaylists.ItemClick -= FeaturedPlaylists_ItemClick;
                    FeaturedPlaylists.ClearValue(XYFocusUpProperty);

                    while (FeaturedPlaylists.Items.Count > 0)
                    {
                        PlaylistHero playlistHero = FeaturedPlaylists.Items.ElementAt(0) as PlaylistHero;
                        FeaturedPlaylists.Items.Remove(playlistHero);
                        playlistHero.Unload();
                    }

                    Refresh.Click -= Refresh_Click;
                    More.Click -= More_Click;
                });
            }
        }
    }
}
