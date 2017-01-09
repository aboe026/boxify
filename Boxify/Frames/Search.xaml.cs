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
            ResultsLoading.Visibility = Visibility.Collapsed;
            if (playlistResultsSave != null)
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                ResultsLoading.Visibility = Visibility.Collapsed;
                SearchBox.Text = searchSave;
                SearchType.SelectedIndex = searchTypeSave;
                foreach (PlaylistList playlist in playlistResultsSave)
                {
                    Results.Items.Add(playlist);
                }
            }
            else if (trackResultsSave != null)
            {
                RelativePanel.SetAlignTopWithPanel(SearchBox, true);
                Results.Visibility = Visibility.Visible;
                ResultsLoading.Visibility = Visibility.Collapsed;
                SearchBox.Text = searchSave;
                SearchType.SelectedIndex = searchTypeSave;
                foreach (TrackList track in trackResultsSave)
                {
                    Results.Items.Add(track);
                }
            }
            else
            {
                Results.Visibility = Visibility.Collapsed;
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
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
                queryParams.Add(new KeyValuePair<string, string>("type", selectedString));
                queryParams.Add(new KeyValuePair<string, string>("q", SearchBox.Text.Replace(" ", "+")));
                string queryParamsString = RequestHandler.convertToQueryString(queryParams);
                searchUriBuilder.Query = queryParamsString;
                string searchResultString = await RequestHandler.sendCliGetRequest(searchUriBuilder.Uri.ToString());
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
                    IJsonValue playlistsJson;
                    IJsonValue itemsJson;
                    if (searchResultJson.TryGetValue("playlists", out playlistsJson))
                    {
                        JsonObject playlists = playlistsJson.GetObject();
                        if (playlists.TryGetValue("items", out itemsJson))
                        {
                            JsonArray playlistsArray = itemsJson.GetArray();
                            if (playlistsArray.Count == 0)
                            {
                                feedbackMessage = "No playlists found.";
                            }
                            else
                            {
                                playlistResultsSave = new List<PlaylistList>();
                                ResultsLoading.Maximum = playlistsArray.Count;
                                ResultsLoading.Value = 0;
                                ResultsLoading.Visibility = Visibility.Visible;
                                foreach (JsonValue playlistJson in playlistsArray)
                                {

                                    Playlist playlist = new Playlist();
                                    await playlist.setInfo(playlistJson.Stringify());
                                    PlaylistList playlistList = new PlaylistList(playlist, mainPage);
                                    if (playlistResultsSave != null)
                                    {
                                        Results.Items.Add(playlistList);
                                        playlistResultsSave.Add(playlistList);
                                    }
                                    ResultsLoading.Value = Results.Items.Count;
                                }
                                ResultsLoading.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
                
                // track
                else if (selectedString == "track")
                {
                    IJsonValue tracksJson;
                    IJsonValue itemsJson;
                    if (searchResultJson.TryGetValue("tracks", out tracksJson))
                    {
                        JsonObject tracks = tracksJson.GetObject();
                        if (tracks.TryGetValue("items", out itemsJson))
                        {
                            JsonArray tracksArray = itemsJson.GetArray();
                            if (tracksArray.Count == 0)
                            {
                                feedbackMessage = "No tracks found.";
                            }
                            else
                            {
                                trackResultsSave = new List<TrackList>();
                                ResultsLoading.Maximum = tracksArray.Count;
                                ResultsLoading.Value = 0;
                                ResultsLoading.Visibility = Visibility.Visible;
                                foreach (JsonValue trackJson in tracksArray)
                                {
                                    Track track = new Track();
                                    await track.setInfoDirect(trackJson.Stringify());
                                    TrackList trackList = new TrackList(track, mainPage);
                                    Results.Items.Add(trackList);
                                    trackResultsSave.Add(trackList);
                                    ResultsLoading.Value = Results.Items.Count;
                                }
                                ResultsLoading.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }

                // album
                else if (selectedString == "album")
                {
                    IJsonValue albumsJson;
                    IJsonValue itemsJson;
                    if (searchResultJson.TryGetValue("albums", out albumsJson))
                    {
                        JsonObject albums = albumsJson.GetObject();
                        if (albums.TryGetValue("items", out itemsJson))
                        {
                            JsonArray albumsArray = itemsJson.GetArray();
                            if (albumsArray.Count == 0)
                            {
                                feedbackMessage = "No albums found.";
                            }
                            else
                            {
                                albumResultsSave = new List<AlbumList>();
                                ResultsLoading.Maximum = albumsArray.Count;
                                ResultsLoading.Value = 0;
                                ResultsLoading.Visibility = Visibility.Visible;
                                foreach (JsonValue albumJson in albumsArray)
                                {
                                    Album album = new Album();
                                    await album.setInfo(albumJson.Stringify());
                                    AlbumList trackList = new AlbumList(album, mainPage);
                                    Results.Items.Add(trackList);
                                    albumResultsSave.Add(trackList);
                                    ResultsLoading.Value = Results.Items.Count;
                                }
                                ResultsLoading.Visibility = Visibility.Collapsed;
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
        private async void Results_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TrackList)
            {
                (e.ClickedItem as TrackList).track.playTrack();
            }
            else if (e.ClickedItem is PlaylistList)
            {
                await (e.ClickedItem as PlaylistList).playlist.playTracks();
            }
            else if (e.ClickedItem is AlbumList) {
                await (e.ClickedItem as AlbumList).album.playTracks();
            }
        }
    }
}
