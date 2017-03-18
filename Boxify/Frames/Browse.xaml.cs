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
                FeaturedPlaylistMessage.Text = featuredPlaylistsMessageSave;
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
            base.OnNavigatedFrom(e);
            FeaturedPlaylists.Items.Clear();
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
            Refresh.IsEnabled = false;
            mainPage.SetSpotifyLoadingValue(0);
            mainPage.BringUpSpotify();
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
                featuredPlaylistsMessageSave = FeaturedPlaylistMessage.Text;
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
                    mainPage.SetSpotifyLoadingMaximum(playlistsArray.Count);
                    if (featuredPlaylistsSave == null)
                    {
                        featuredPlaylistsSave = new List<PlaylistHero>();
                    }
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        if (playlistJson.GetObject().TryGetValue("href", out IJsonValue fullHref))
                        {
                            string fullPlaylistString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                            Playlist playlist = new Playlist();
                            await playlist.SetInfo(fullPlaylistString);
                            PlaylistHero playlistHero = new PlaylistHero(playlist, mainPage);
                            FeaturedPlaylists.Items.Add(playlistHero);
                            featuredPlaylistsSave.Add(playlistHero);
                            mainPage.SetSpotifyLoadingValue(featuredPlaylistsSave.Count);
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
            featuredPlaylistsSave = new List<PlaylistHero>();
            FeaturedPlaylists.Items.Clear();
            await LoadFeaturedPlaylists();
        }

        /// <summary>
        /// User clicks a playlist to play the tracks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FeaturedPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as PlaylistHero).Playlist.PlayTracks();
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
        /// free memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                featuredPlaylistsOffset = 0;
                featuredPlaylistsSave = null;
            }
        }
    }
}
