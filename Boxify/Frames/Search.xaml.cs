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
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        public static string searchSave;
        public static int searchTypeSave;
        public const String SEARCH_URL = "https://api.spotify.com/v1/search";

        public Search()
        {
            this.InitializeComponent();
            MainPage.searchPage = this;
            Feedback.Text = "";

            if (searchSave != null)
            {
                SearchBox.Text = searchSave;
                SearchType.SelectionChanged -= SearchButton_Click;
                SearchType.SelectedIndex = searchTypeSave;
                SearchType.SelectionChanged += SearchButton_Click;
                SearchButton_Click(SearchButton, null);
            }
        }

        /// <summary>
        /// Search for the selected item in Spotify
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string feedbackMessage = "";
            if (Feedback == null)
            {
                return;
            }
            Feedback.Text = "";
            feedbackMessage = "";
            if (SearchBox.Text == "")
            {
                feedbackMessage = "Please enter text to search for (I can't read your mind...yet)";
            }
            else
            {
                searchSave = SearchBox.Text;
                searchTypeSave = SearchType.SelectedIndex;
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                ComboBoxItem selected = SearchType.SelectedValue as ComboBoxItem;
                String selectedString = selected.Content.ToString().ToLower();
                UriBuilder searchUriBuilder = new UriBuilder(SEARCH_URL);
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("type", selectedString),
                    new KeyValuePair<string, string>("limit", "10"),
                    new KeyValuePair<string, string>("q", SearchBox.Text.Replace(" ", "+"))
                };
                string queryParamsString = RequestHandler.ConvertToQueryString(queryParams);
                searchUriBuilder.Query = queryParamsString;
                string searchResultString = await RequestHandler.SendCliGetRequest(searchUriBuilder.Uri.ToString());
                JsonObject searchResultJson = new JsonObject();
                try
                {
                    searchResultJson = JsonObject.Parse(searchResultString);
                }
                catch (COMException)
                {
                    return;
                }

                clearResults();
                Results.Visibility = Visibility.Visible;

                // playlists
                if (selectedString == "playlist")
                {
                    if (searchResultJson.TryGetValue("playlists", out IJsonValue playlistsJson))
                    {
                        JsonObject playlists = playlistsJson.GetObject();
                        if (playlists.TryGetValue("items", out IJsonValue itemsJson))
                        {
                            JsonArray playlistsArray = itemsJson.GetArray();
                            if (playlistsArray.Count == 0)
                            {
                                feedbackMessage = "No playlists found.";
                            }
                            else
                            {
                                App.mainPage.SetSpotifyLoadingMaximum(playlistsArray.Count);
                                App.mainPage.SetSpotifyLoadingValue(0);
                                App.mainPage.BringUpSpotify();
                                foreach (JsonValue playlistJson in playlistsArray)
                                {

                                    Playlist playlist = new Playlist();
                                    await playlist.SetInfo(playlistJson.Stringify());
                                    PlaylistList playlistList = new PlaylistList(playlist);
                                    try
                                    {
                                        if (!App.isInBackgroundMode)
                                        {
                                            Results.Items.Add(playlistList);
                                        }
                                    }
                                    catch (COMException) { }
                                    App.mainPage.SetSpotifyLoadingValue(Results.Items.Count);
                                }
                            }
                        }
                    }
                }

                // track
                else if (selectedString == "track")
                {
                    if (searchResultJson.TryGetValue("tracks", out IJsonValue tracksJson))
                    {
                        JsonObject tracks = tracksJson.GetObject();
                        if (tracks.TryGetValue("items", out IJsonValue itemsJson))
                        {
                            JsonArray tracksArray = itemsJson.GetArray();
                            if (tracksArray.Count == 0)
                            {
                                feedbackMessage = "No tracks found.";
                            }
                            else
                            {
                                App.mainPage.SetSpotifyLoadingMaximum(tracksArray.Count);
                                App.mainPage.SetSpotifyLoadingValue(0);
                                App.mainPage.BringUpSpotify();
                                foreach (JsonValue trackJson in tracksArray)
                                {
                                    Track track = new Track();
                                    await track.SetInfoDirect(trackJson.Stringify());
                                    TrackList trackList = new TrackList(track);
                                    try
                                    {
                                        if (!App.isInBackgroundMode)
                                        {
                                            Results.Items.Add(trackList);
                                        }
                                    }
                                    catch (COMException) { }
                                    App.mainPage.SetSpotifyLoadingValue(Results.Items.Count);
                                }
                            }
                        }
                    }
                }

                // album
                else if (selectedString == "album")
                {
                    if (searchResultJson.TryGetValue("albums", out IJsonValue albumsJson))
                    {
                        JsonObject albums = albumsJson.GetObject();
                        if (albums.TryGetValue("items", out IJsonValue itemsJson))
                        {
                            JsonArray albumsArray = itemsJson.GetArray();
                            if (albumsArray.Count == 0)
                            {
                                feedbackMessage = "No albums found.";
                            }
                            else
                            {
                                App.mainPage.SetSpotifyLoadingMaximum(albumsArray.Count);
                                App.mainPage.SetSpotifyLoadingValue(0);
                                App.mainPage.BringUpSpotify();
                                foreach (JsonValue albumJson in albumsArray)
                                {
                                    Album album = new Album();
                                    await album.SetInfo(albumJson.Stringify());
                                    AlbumList albumList = new AlbumList(album);
                                    try
                                    {
                                        if (!App.isInBackgroundMode)
                                        {
                                            Results.Items.Add(albumList);
                                        }
                                    }
                                    catch (COMException) { }
                                    App.mainPage.SetSpotifyLoadingValue(Results.Items.Count);
                                }
                            }
                        }
                    }
                }
            }
            Feedback.Text = feedbackMessage;
            if (feedbackMessage == "")
            {
                Feedback.Visibility = Visibility.Collapsed;
            }
            else
            {
                Feedback.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// User clicks result item to be played
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Results_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackList)
            {
                (e.ClickedItem as TrackList).track.PlayTrack();
            }
            else if (e.ClickedItem is PlaylistList)
            {
                (e.ClickedItem as PlaylistList).playlist.PlayTracks();
            }
            else if (e.ClickedItem is AlbumList)
            {
                (e.ClickedItem as AlbumList).album.PlayTracks();
            }
        }

        /// <summary>
        /// Clears the search results objects to purge them from memory
        /// </summary>
        private void clearResults()
        {
            while (Results.Items.Count > 0)
            {
                object listItem = Results.Items.ElementAt(0);
                if (listItem is PlaylistList)
                {
                    PlaylistList playlistList = listItem as PlaylistList;
                    playlistList.Unload();
                    Results.Items.Remove(playlistList);
                    playlistList = null;
                }
                else if (listItem is TrackList)
                {
                    TrackList trackList = listItem as TrackList;
                    trackList.Unload();
                    Results.Items.Remove(trackList);
                    trackList = null;
                }
                else if (listItem is AlbumList)
                {
                    AlbumList albumList = listItem as AlbumList;
                    albumList.Unload();
                    Results.Items.Remove(albumList);
                    albumList = null;
                }
            }
        }

        /// <summary>
        /// user hits enter to search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SearchButton_Click(null, null);
            }
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
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Results != null)
                    {
                        Results.ItemClick -= Results_ItemClick;
                        clearResults();
                        Results = null;
                    }
                    if (SearchType != null)
                    {
                        SearchType.SelectionChanged -= SearchButton_Click;
                        SearchType = null;
                    }
                    if (SearchButton != null)
                    {
                        SearchButton.Click -= SearchButton_Click;
                        SearchButton = null;
                    }

                    SearchBox = null;
                    Feedback = null;
                });
            }
        }
    }
}
