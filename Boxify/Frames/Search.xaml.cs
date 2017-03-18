using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        public static MainPage mainPage;
        public static String feedbackMessage = "";
        public static String searchUri = "https://api.spotify.com/v1/search";
        public static String searchSave = "";
        public static int searchTypeSave;
        private static List<PlaylistList> playlistResultsSave;
        private static List<TrackList> trackResultsSave;
        private static List<AlbumList> albumResultsSave;

        public Search()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }
            Feedback.Text = feedbackMessage;
            if (playlistResultsSave != null)
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                SearchBox.Text = searchSave;
                SearchType.SelectionChanged -= SearchButton_Click;
                SearchType.SelectedIndex = searchTypeSave;
                SearchType.SelectionChanged += SearchButton_Click;
                foreach (PlaylistList playlist in playlistResultsSave)
                {
                    try
                    {
                        Results.Items.Add(playlist);
                    }
                    catch (COMException) { }
                }
            }
            else if (trackResultsSave != null)
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                SearchBox.Text = searchSave;
                SearchType.SelectionChanged -= SearchButton_Click;
                SearchType.SelectedIndex = searchTypeSave;
                SearchType.SelectionChanged += SearchButton_Click;
                foreach (TrackList track in trackResultsSave)
                {
                    try
                    {
                        Results.Items.Add(track);
                    }
                    catch (COMException) { }
                }
            }
            else if (albumResultsSave != null)
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                SearchBox.Text = searchSave;
                SearchType.SelectionChanged -= SearchButton_Click;
                SearchType.SelectedIndex = searchTypeSave;
                SearchType.SelectionChanged += SearchButton_Click;
                foreach (AlbumList album in albumResultsSave)
                {
                    try
                    {
                        Results.Items.Add(album);
                    }
                    catch (COMException) { }
                }
            }
            else if (playlistResultsSave == null && trackResultsSave == null && albumResultsSave == null && searchSave != "")
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                SearchBox.Text = searchSave;
                SearchType.SelectionChanged -= SearchButton_Click;
                SearchType.SelectedIndex = searchTypeSave;
                SearchType.SelectionChanged += SearchButton_Click;
                SearchButton_Click(SearchButton, null);
            }
        }

        /// <summary>
        /// When a user leaves the page
        /// </summary>
        /// <param name="e">The naviagation event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Results.Items.Clear();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Search for the selected item in Spotify
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
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
                searchTypeSave = SearchType.SelectedIndex;
                searchSave = SearchBox.Text;

                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                ComboBoxItem selected = SearchType.SelectedValue as ComboBoxItem;
                String selectedString = selected.Content.ToString().ToLower();
                UriBuilder searchUriBuilder = new UriBuilder(searchUri);
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

                Results.Items.Clear();
                playlistResultsSave = null;
                trackResultsSave = null;
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
                                playlistResultsSave = new List<PlaylistList>();
                                mainPage.SetSpotifyLoadingMaximum(playlistsArray.Count);
                                mainPage.SetSpotifyLoadingValue(0);
                                mainPage.BringUpSpotify();
                                foreach (JsonValue playlistJson in playlistsArray)
                                {

                                    Playlist playlist = new Playlist();
                                    await playlist.SetInfo(playlistJson.Stringify());
                                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                                    try
                                    {
                                        Results.Items.Add(playlistList);
                                    }
                                    catch (COMException) { }
                                    playlistResultsSave.Add(playlistList);
                                    mainPage.SetSpotifyLoadingValue(Results.Items.Count);
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
                                trackResultsSave = new List<TrackList>();
                                mainPage.SetSpotifyLoadingMaximum(tracksArray.Count);
                                mainPage.SetSpotifyLoadingValue(0);
                                mainPage.BringUpSpotify();
                                foreach (JsonValue trackJson in tracksArray)
                                {
                                    Track track = new Track();
                                    await track.SetInfoDirect(trackJson.Stringify());
                                    TrackList trackList = new TrackList(track, mainPage);
                                    try
                                    {
                                        Results.Items.Add(trackList);
                                    }
                                    catch (COMException) { }
                                    trackResultsSave.Add(trackList);
                                    mainPage.SetSpotifyLoadingValue(Results.Items.Count);
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
                                albumResultsSave = new List<AlbumList>();
                                mainPage.SetSpotifyLoadingMaximum(albumsArray.Count);
                                mainPage.SetSpotifyLoadingValue(0);
                                mainPage.BringUpSpotify();
                                foreach (JsonValue albumJson in albumsArray)
                                {
                                    Album album = new Album();
                                    await album.SetInfo(albumJson.Stringify());
                                    AlbumList albumList = new AlbumList(album, mainPage);
                                    try
                                    {
                                        Results.Items.Add(albumList);
                                    }
                                    catch (COMException) { }
                                    albumResultsSave.Add(albumList);
                                    mainPage.SetSpotifyLoadingValue(Results.Items.Count);
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
                (e.ClickedItem as TrackList).Track.PlayTrack();
            }
            else if (e.ClickedItem is PlaylistList)
            {
                (e.ClickedItem as PlaylistList).Playlist.PlayTracks();
            }
            else if (e.ClickedItem is AlbumList) {
                (e.ClickedItem as AlbumList).Album.PlayTracks();
            }
        }

        /// <summary>
        /// Used when freeing memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                playlistResultsSave = null;
                trackResultsSave = null;
                albumResultsSave = null;
            }
        }
    }
}
