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

namespace Boxify
{
    /// <summary>
    /// The page displaying the users custom music selections
    /// </summary>
    public sealed partial class YourMusic : Page
    {
        private static string playlistsHref = "https://api.spotify.com/v1/me/playlists";
        private static int playlistLimit = 10;
        private static int playlistsOffset = 0;
        private static int playlistsTotal = 20;
        private static int playlistsCount = 0;
        public static bool refreshing = false;
        public static List<PlaylistList> preEmptiveLoadPlaylists = new List<PlaylistList>();

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
            if (e.Parameter is MainPage)
            {
                App.mainPage = e.Parameter as MainPage;
                MainPage.yourMusicPage = this;
            }
            if (UserProfile.IsLoggedIn())
            {
                More.IsEnabled = false;
                Warning.Visibility = Visibility.Collapsed;
                LogIn.Visibility = Visibility.Collapsed;
                if (refreshing)
                {
                    Refresh.Visibility = Visibility.Collapsed;
                    App.mainPage.SetSpotifyLoadingMaximum(playlistsCount);
                    App.mainPage.SetSpotifyLoadingValue(0);
                    App.mainPage.BringUpSpotify();
                    while (refreshing)
                    {
                        App.mainPage.SetSpotifyLoadingMaximum(playlistsCount);
                        App.mainPage.SetSpotifyLoadingValue(preEmptiveLoadPlaylists.Count);
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                    App.mainPage.SetSpotifyLoadingValue(playlistsCount);
                }
                if (preEmptiveLoadPlaylists.Count > 0)
                {
                    foreach (PlaylistList playlist in preEmptiveLoadPlaylists)
                    {
                        try
                        {
                            Playlists.Items.Add(playlist);
                        }
                        catch (COMException) { }
                    }
                    Refresh.Visibility = Visibility.Visible;
                    preEmptiveLoadPlaylists.Clear();
                }
                else if (Playlists.Items.Count == 0)
                {
                    await LoadPlaylists();
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
                PlaylistsLabel.Visibility = Visibility.Collapsed;
                Refresh.Visibility = Visibility.Collapsed;
                Warning.Visibility = Visibility.Visible;
                LogIn.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Refreshes the users playlists
        /// </summary>
        /// <returns></returns>
        public async Task LoadPlaylists()
        {
            More.IsEnabled = false;
            Refresh.IsEnabled = false;
            App.mainPage.SetSpotifyLoadingValue(0);
            App.mainPage.BringUpSpotify();

            UriBuilder playlistsBuilder = new UriBuilder(playlistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", playlistLimit.ToString()),
                new KeyValuePair<string, string>("offset", playlistsOffset.ToString())
            };
            playlistsBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
            string playlistsString = await RequestHandler.SendAuthGetRequest(playlistsBuilder.Uri.ToString());
            JsonObject playlistsJson = new JsonObject();
            try
            {
                playlistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            if (playlistsJson.TryGetValue("total", out IJsonValue totalJson))
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out IJsonValue itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                App.mainPage.SetSpotifyLoadingMaximum(playlistsArray.Count);
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.SetInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist);
                    Playlists.Items.Add(playlistList);
                    App.mainPage.SetSpotifyLoadingValue(Playlists.Items.Count);
                }
            }
            Refresh.IsEnabled = true;
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
        public static async Task SetPlaylists()
        {
            UriBuilder playlistsBuilder = new UriBuilder(playlistsHref);
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", playlistLimit.ToString()),
                new KeyValuePair<string, string>("offset", playlistsOffset.ToString())
            };
            playlistsBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
            string playlistsString = await RequestHandler.SendAuthGetRequest(playlistsBuilder.Uri.ToString());
            JsonObject playlistsJson = new JsonObject();
            try
            {
                playlistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            if (playlistsJson.TryGetValue("total", out IJsonValue totalJson))
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out IJsonValue itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                playlistsCount = playlistsArray.Count;
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    Playlist playlist = new Playlist();
                    await playlist.SetInfo(playlistJson.Stringify());
                    PlaylistList playlistList = new PlaylistList(playlist);
                    preEmptiveLoadPlaylists.Add(playlistList);
                }
            }
        }

        /// <summary>
        /// Send a user to the login page
        /// </summary>
        /// <param name="sender">The object that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            App.mainPage.SelectHamburgerOption("ProfileItem", true);
        }

        /// <summary>
        /// Refresh users playlists
        /// </summary>
        /// <param name="sender">The refresh button</param>
        /// <param name="e">The routed event arguments</param>
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            playlistsOffset = 0;
            Playlists.Items.Clear();
            await LoadPlaylists();
        }

        /// <summary>
        /// When user clicks a PlaylistList for playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlists_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as PlaylistList).Playlist.PlayTracks();
        }

        /// <summary>
        /// User wishes to load more of their playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void More_Click(object sender, RoutedEventArgs e)
        {
            Playlists.Focus(FocusState.Programmatic);
            playlistsOffset += playlistLimit;
            await LoadPlaylists();
        }

        /// <summary>
        /// Used when freeing memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                playlistsOffset = 0;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Bindings.StopTracking();

                    if (Playlists != null)
                    {
                        Playlists.ItemClick -= Playlists_ItemClick;
                        while (Playlists.Items.Count > 0)
                        {
                            PlaylistList playlistList = Playlists.Items.ElementAt(0) as PlaylistList;
                            playlistList.Unload();
                            Playlists.Items.Remove(playlistList);
                            playlistList = null;
                        }
                        Playlists = null;
                    }

                    if (LogIn != null)
                    {
                        LogIn.Click -= LogIn_Click;
                        LogIn = null;
                    }
                    if (Refresh != null)
                    {
                        Refresh.Click -= Refresh_Click;
                        Refresh = null;
                    }
                    if (More != null)
                    {
                        More.Click -= More_Click;
                        More = null;
                    }


                    Warning = null;
                    PlaylistsLabel = null;

                });
            }
        }
    }
}
