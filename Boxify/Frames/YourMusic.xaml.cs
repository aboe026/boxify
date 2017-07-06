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
using Boxify.Classes;
using static Boxify.Frames.Settings;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
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
            MainPage.yourMusicPage = this;
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            PlaylistsHeader.Visibility = Visibility.Collapsed;
            EmptyMessage.Visibility = Visibility.Collapsed;
            if (UserProfile.IsLoggedIn())
            {
                PlaylistsRefresh.IsEnabled = false;
                PlaylistsMore.IsEnabled = false;
                Warning.Visibility = Visibility.Collapsed;
                LogIn.Visibility = Visibility.Collapsed;
                MainPivot.Visibility = Visibility.Visible;
                if (refreshing)
                {
                    PlaylistsHeader.Visibility = Visibility.Visible;
                    long loadingKey = DateTime.Now.Ticks;
                    MainPage.AddLoadingLock(loadingKey);
                    App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, 0, playlistsCount, loadingKey);
                    while (refreshing)
                    {
                        App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, preEmptiveLoadPlaylists.Count, playlistsCount, loadingKey);
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                    App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, playlistsCount, playlistsCount, loadingKey);
                    MainPage.RemoveLoadingLock(loadingKey);
                }
                if (preEmptiveLoadPlaylists.Count > 0)
                {
                    PlaylistsHeader.Visibility = Visibility.Visible;
                    foreach (PlaylistList playlist in preEmptiveLoadPlaylists)
                    {
                        try
                        {
                            PlaylistsView.Items.Add(playlist);
                        }
                        catch (COMException) { }
                    }
                    preEmptiveLoadPlaylists.Clear();
                }
                else if (PlaylistsView.Items.Count == 0)
                {
                    await LoadPlaylists();
                }

                PlaylistsRefresh.IsEnabled = true;
                if (playlistsOffset + playlistLimit >= playlistsTotal)
                {
                    PlaylistsMore.Content = "No More";
                    PlaylistsMore.IsEnabled = false;
                }
                else
                {
                    PlaylistsMore.Content = "More";
                    PlaylistsMore.IsEnabled = true;
                }
            }
            else
            {
                MainPivot.Visibility = Visibility.Collapsed;
                Warning.Visibility = Visibility.Visible;
                LogIn.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// User changes tab being viewed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// Refreshes the users playlists
        /// </summary>
        /// <returns></returns>
        public async Task LoadPlaylists()
        {
            PlaylistsHeader.Visibility = Visibility.Collapsed;
            EmptyMessage.Visibility = Visibility.Collapsed;
            PlaylistsMore.IsEnabled = false;
            PlaylistsRefresh.IsEnabled = false;
            long loadingKey = DateTime.Now.Ticks;
            MainPage.AddLoadingLock(loadingKey);
            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, 0, 1, loadingKey);

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
            if (playlistsJson.TryGetValue("total", out IJsonValue totalJson) && totalJson.ValueType == JsonValueType.Number)
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out IJsonValue itemsJson) && itemsJson.ValueType == JsonValueType.Array)
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                if (playlistsArray.Count == 0)
                {
                    PlaylistsHeader.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Visible;
                    App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, 1, 1, loadingKey);
                }
                else
                {
                    PlaylistsHeader.Visibility = Visibility.Visible;
                    foreach (JsonValue playlistJson in playlistsArray)
                    {
                        if (playlistJson.GetObject().TryGetValue("href", out IJsonValue fullHref) && fullHref.ValueType == JsonValueType.String)
                        {
                            string fullPlaylistString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                            Playlist playlist = new Playlist();
                            await playlist.SetInfo(fullPlaylistString);
                            PlaylistList playlistList = new PlaylistList(playlist);
                            PlaylistsView.Items.Add(playlistList);
                            if (PlaylistsView.Items.IndexOf(playlistList) % 2 == 1)
                            {
                                playlistList.TurnOffOpaqueBackground();
                            }
                            App.mainPage.SetLoadingProgress(PlaybackSource.Spotify, PlaylistsView.Items.Count, playlistsArray.Count, loadingKey);
                        }
                    }
                }
            }
            PlaylistsRefresh.IsEnabled = true;
            if (playlistsOffset + playlistLimit >= playlistsTotal)
            {
                PlaylistsMore.Content = "No More";
                PlaylistsMore.IsEnabled = false;
            }
            else
            {
                PlaylistsMore.Content = "More";
                PlaylistsMore.IsEnabled = true;
            }

            MainPage.RemoveLoadingLock(loadingKey);
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
            if (playlistsJson.TryGetValue("total", out IJsonValue totalJson) && totalJson.ValueType == JsonValueType.Number)
            {
                playlistsTotal = Convert.ToInt32(totalJson.GetNumber());
            }
            if (playlistsJson.TryGetValue("items", out IJsonValue itemsJson) && itemsJson.ValueType == JsonValueType.Array)
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                playlistsCount = playlistsArray.Count;
                foreach (JsonValue playlistJson in playlistsArray)
                {
                    if (playlistJson.GetObject().TryGetValue("href", out IJsonValue fullHref) && fullHref.ValueType == JsonValueType.String)
                    {
                        string fullPlaylistString = await RequestHandler.SendCliGetRequest(fullHref.GetString());
                        Playlist playlist = new Playlist();
                        await playlist.SetInfo(fullPlaylistString);
                        PlaylistList playlistList = new PlaylistList(playlist);
                        preEmptiveLoadPlaylists.Add(playlistList);
                        if (preEmptiveLoadPlaylists.IndexOf(playlistList) % 2 == 1)
                        {
                            playlistList.TurnOffOpaqueBackground();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears UI playlists
        /// </summary>
        public void ClearPlaylists()
        {
            playlistsOffset = 0;
            while (PlaylistsView.Items.Count > 0)
            {
                PlaylistList playlistList = PlaylistsView.Items.ElementAt(0) as PlaylistList;
                playlistList.Unload();
                PlaylistsView.Items.Remove(playlistList);
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
        private async void PlaylistsRefresh_Click(object sender, RoutedEventArgs e)
        {
            ClearPlaylists();
            await LoadPlaylists();
        }

        /// <summary>
        /// When user clicks a PlaylistList for playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (e.ClickedItem as PlaylistList).playlist.PlayTracks();
        }

        /// <summary>
        /// User wishes to load more of their playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PlaylistsMore_Click(object sender, RoutedEventArgs e)
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

                    PlaylistsView.ItemClick -= PlaylistsView_ItemClick;
                    ClearPlaylists();

                    LogIn.Click -= LogIn_Click;
                    PlaylistsRefresh.Click -= PlaylistsRefresh_Click;
                    PlaylistsMore.Click -= PlaylistsMore_Click;

                    while (preEmptiveLoadPlaylists.Count > 0)
                    {
                        PlaylistList playlistList = preEmptiveLoadPlaylists.ElementAt(0) as PlaylistList;
                        playlistList.Unload();
                        preEmptiveLoadPlaylists.Remove(playlistList);
                    }
                });
            }
        }
    }
}
