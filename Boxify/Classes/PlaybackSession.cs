using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VideoLibrary;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using static Boxify.Settings;

namespace Boxify.Classes
{
    public class PlaybackSession : IDisposable
    {
        public enum LoadDirection { Up, Down };
        public enum PlaybackType { Single, Album, Playlist };

        public static string youtubeApplicationName = "";
        public static string youtubeApiKey = "";
        private const string _videoUrlFormat = "http://www.youtube.com/watch?v={0}";

        public long localLock;
        private Playbacksource source = Playbacksource.Spotify;
        private PlaybackType type;
        private string tracksHref;
        private List<int> nextRemoteAttempts = new List<int>();
        private List<int> prevRemoteAttempts = new List<int>();
        private List<string> playlistMediaIds = new List<string>();
        private string currentlyPlaying = "";
        private int totalTracks;
        private bool firstSongChange = true;  // the first song change is from playing the first track. Its not an actual "change"
        private bool loadLock = false;
        private int failuresCount = 0;

        public const int INITIAL_TRACKS_REQUEST = 3;
        private const int TRACKS_PER_REQUEST = 2;
        private const int BUFFER_FROM_LOAD = 2;

        /// <summary>
        /// The main constructor
        /// </summary>
        /// <param name="currentLock">The global lock when the session was created, session dies when lock changes</param>
        /// <param name="currentSource">The service to retrieve the song from</param>
        /// <param name="playbackType">What kind of Spotify resource</param>
        /// <param name="href">The reference to the download location for tracks</param>
        public PlaybackSession(long currentLock, Playbacksource currentSource, PlaybackType playbackType, string href)
        {
            localLock = currentLock;
            source = currentSource;
            type = playbackType;
            tracksHref = href;
            if (type == PlaybackType.Single)
            {
                totalTracks = 1;
            }
            prevRemoteAttempts.Add(0);
        }

        /// <summary>
        /// Update the UI with the current number of tracks that have failed
        /// </summary>
        /// <param name="increment"></param>
        private async void UpdateFailuresCount(int increment)
        {
            failuresCount += increment;

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlaybackService.mainPage.SetYouTubeMessage(failuresCount + " track" + (failuresCount == 1 ? "" : "s") + " failed to match", localLock);
            });
        }

        /// <summary>
        /// Load the range of tracks from the remote playlist
        /// </summary>
        /// <param name="start">The first track to load from remote</param>
        /// <param name="end">The last track to load from remote</param>
        /// <returns>True a total of end - start tracks are downloaded, false otherwise</returns>
        public async Task<bool> LoadTracks(int start, int end)
        {
            return await LoadTracks(start, end, false);
        }

        /// <summary>
        /// Load the range of tracks from the remote playlist
        /// </summary>
        /// <param name="start">The first track to load from remote</param>
        /// <param name="end">The last track to load from remote</param>
        /// <param name="lockOverride">Whether or not to ignore if the lock is set (Used when recursing into itself)</param>
        /// <returns>True a total of end - start tracks are downloaded, false otherwise</returns>
        public async Task<bool> LoadTracks(int start, int end, bool lockOverride)
        {
            if (loadLock && !lockOverride)
            {
                return false;
            }
            loadLock = true;
            int successes = 0;

            if (totalTracks > 0 && end >= totalTracks)
            {
                end = totalTracks - 1;
            }
            nextRemoteAttempts.Insert(0, end);
            if (start == 0)
            {
                prevRemoteAttempts.Add(start + TRACKS_PER_REQUEST);
            }
            else
            {
                prevRemoteAttempts.Add(start);
            }

            int limit = end - start + 1;

            if (this.source == Playbacksource.Spotify)
            {
                PlaybackService.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                PlaybackService.mainPage.SetSpotifyLoadingValue(0, localLock);
                PlaybackService.mainPage.BringUpSpotify(localLock);
            }
            else if (this.source == Playbacksource.YouTube)
            {
                PlaybackService.mainPage.SetYouTubeLoadingMaximum(limit, localLock);
                PlaybackService.mainPage.SetYouTubeLoadingValue(0, localLock);
                PlaybackService.mainPage.BringUpYouTube(localLock);
            }

            if (localLock != PlaybackService.globalLock) { return false; }

            List<Track> tracks = await GetTracks(start, limit);

            if (tracks.Count == totalTracks)
            {
                limit = totalTracks;
                if (this.source == Playbacksource.Spotify)
                {
                    PlaybackService.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                }
                else if (this.source == Playbacksource.YouTube)
                {
                    PlaybackService.mainPage.SetYouTubeLoadingMaximum(limit, localLock);
                }
            }

            if (tracks.Count != limit)
            {
                UpdateFailuresCount(limit - tracks.Count);
            }
            else
            {
                List<KeyValuePair<MediaSource, Track>> sources = new List<KeyValuePair<MediaSource, Track>>();

                if (this.source == Playbacksource.Spotify)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];
                        if (localLock == PlaybackService.globalLock)
                        {
                            if (track.previewUrl != "")
                            {
                                sources.Add(new KeyValuePair<MediaSource, Track>(MediaSource.CreateFromUri(new Uri(track.previewUrl)), track));
                            }
                            else
                            {
                                UpdateFailuresCount(1);
                            }
                        }
                        PlaybackService.mainPage.SetSpotifyLoadingValue(i + 1 + limit - tracks.Count, localLock);
                    }
                }
                else if (this.source == Playbacksource.YouTube && localLock == PlaybackService.globalLock)
                {
                    Dictionary<Track, string> videoIds = await BulkSearchForVideoId(tracks);

                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];
                        if (localLock == PlaybackService.globalLock)
                        {
                            try
                            {
                                sources.Add(new KeyValuePair<MediaSource, Track>(await GetAudioAsync(videoIds[track], track.Name), track));
                            }
                            catch (Exception)
                            {
                                UpdateFailuresCount(1);
                            }
                            PlaybackService.mainPage.SetYouTubeLoadingValue(i + 1 + limit - tracks.Count, localLock);
                        }
                    }
                }

                bool firstPlay = false;

                for (int i = 0; i < sources.Count; i++)
                {
                    KeyValuePair<MediaSource, Track> pair = sources[i];
                    if (localLock == PlaybackService.globalLock)
                    {
                        MediaPlaybackItem playbackItem = new MediaPlaybackItem(pair.Key);
                        MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                        displayProperties.Type = MediaPlaybackType.Music;
                        displayProperties.MusicProperties.Title = pair.Value.name;
                        displayProperties.MusicProperties.AlbumTitle = pair.Value.album.name;
                        displayProperties.MusicProperties.Artist = pair.Value.ArtistName;
                        if (pair.Value.album.images.Count > 0 && pair.Value.album.images.ElementAt(0) != null)
                        {
                            displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(pair.Value.album.imageUrl));
                        }
                        playbackItem.ApplyDisplayProperties(displayProperties);
                        pair.Key.CustomProperties["mediaItemId"] = pair.Value.id;

                        string id = GetMediaItemId(playbackItem);
                        if (!playlistMediaIds.Contains(id))
                        {
                            PlaybackService.addToQueue(playbackItem, localLock);
                            playlistMediaIds.Add(id);
                            successes++;
                        }

                        if (currentlyPlaying == "")
                        {
                            firstPlay = true;
                            currentlyPlaying = GetMediaItemId(playbackItem);
                        }
                    }
                }

                if (firstPlay)
                {
                    PlaybackService.playFromBeginning(localLock);
                }
            }

            if (successes != limit && end < totalTracks)
            {
                return await LoadTracks(start + limit, start + limit + (limit - tracks.Count), true);
            }
            loadLock = false;
            return tracks.Count == limit;
        }

        /// <summary>
        /// Load the range of tracks from the remote playlist in reverse order 
        /// </summary>
        /// <param name="start">The start position on remote to download to local, the last position added to the queue</param>
        /// <param name="end">The end position on remote to download to local, the first position added to the queue</param>
        /// <returns>True a total of end - start tracks are downloaded, false otherwise</returns>
        public async Task<bool> LoadTracksReverse(int start, int end)
        {
            return await LoadTracksReverse(start, end, false);
        }

        /// <summary>
        /// Load the range of tracks from the remote playlist in reverse order 
        /// </summary>
        /// <param name="start">The start position on remote to download to local, the last position added to the queue</param>
        /// <param name="end">The end position on remote to download to local, the first position added to the queue</param>
        /// <param name="lockOverride">Whether or not to ignore if the lock is set (Used when recursing into itself)</param>
        /// <returns>True a total of end - start tracks are downloaded, false otherwise</returns>
        public async Task<bool> LoadTracksReverse(int start, int end, bool lockOverride)
        {
            if (loadLock && !lockOverride)
            {
                return false;
            }
            loadLock = true;
            int successes = 0;

            if (start < 0)
            {
                start = 0;
            }
            nextRemoteAttempts.Add(end);
            prevRemoteAttempts.Insert(0, start);

            int limit = end - start + 1;

            if (this.source == Playbacksource.Spotify)
            {
                PlaybackService.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                PlaybackService.mainPage.SetSpotifyLoadingValue(0, localLock);
                PlaybackService.mainPage.BringUpSpotify(localLock);
            }
            else if (this.source == Playbacksource.YouTube)
            {
                PlaybackService.mainPage.SetYouTubeLoadingMaximum(limit, localLock);
                PlaybackService.mainPage.SetYouTubeLoadingValue(0, localLock);
                PlaybackService.mainPage.BringUpYouTube(localLock);
            }

            if (localLock != PlaybackService.globalLock) { return false; }

            List<Track> tracks = await GetTracks(start, limit);
            if (tracks.Count != limit)
            {
                UpdateFailuresCount(limit - tracks.Count);
            }
            else
            {
                List<KeyValuePair<MediaSource, Track>> sources = new List<KeyValuePair<MediaSource, Track>>();

                if (this.source == Playbacksource.Spotify)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];
                        if (localLock == PlaybackService.globalLock)
                        {
                            if (track.previewUrl != "")
                            {
                                sources.Add(new KeyValuePair<MediaSource, Track>(MediaSource.CreateFromUri(new Uri(track.previewUrl)), track));
                            }
                            else
                            {
                                UpdateFailuresCount(1);
                            }
                        }
                        PlaybackService.mainPage.SetSpotifyLoadingValue(i + 1 + limit - tracks.Count, localLock);
                    }
                }
                else if (this.source == Playbacksource.YouTube && localLock == PlaybackService.globalLock)
                {
                    Dictionary<Track, string> videoIds = await BulkSearchForVideoId(tracks);

                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];
                        if (localLock == PlaybackService.globalLock)
                        {
                            try
                            {
                                sources.Add(new KeyValuePair<MediaSource, Track>(await GetAudioAsync(videoIds[track], track.Name), track));
                            }
                            catch (Exception)
                            {
                                UpdateFailuresCount(1);
                            }
                            PlaybackService.mainPage.SetYouTubeLoadingValue(i + 1 + limit - tracks.Count, localLock);
                        }
                    }
                }

                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<MediaSource, Track> pair = sources[i];
                    if (localLock == PlaybackService.globalLock)
                    {
                        MediaPlaybackItem playbackItem = new MediaPlaybackItem(pair.Key);
                        MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                        displayProperties.Type = MediaPlaybackType.Music;
                        displayProperties.MusicProperties.Title = pair.Value.name;
                        displayProperties.MusicProperties.AlbumTitle = pair.Value.album.name;
                        displayProperties.MusicProperties.Artist = pair.Value.ArtistName;
                        if (pair.Value.album.images.Count > 0 && pair.Value.album.images.ElementAt(0) != null)
                        {
                            displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(pair.Value.album.imageUrl));
                        }
                        playbackItem.ApplyDisplayProperties(displayProperties);
                        pair.Key.CustomProperties["mediaItemId"] = pair.Value.id;

                        string id = GetMediaItemId(playbackItem);
                        if (!playlistMediaIds.Contains(id))
                        {
                            PlaybackService.addToBeginningOfQueue(playbackItem, localLock);
                            playlistMediaIds.Insert(0, id);
                            successes++;
                        }
                    }
                }
            }

            if (successes != limit && start > 0)
            {
                return await LoadTracks(start - limit - (limit - tracks.Count), start + limit, true);
            }
            loadLock = false;
            return tracks.Count == limit;
        }

        /// <summary>
        /// When the queue moves to another song
        /// </summary>
        /// <param name="newItem">The new song being played</param>
        public async void SongChanged(MediaPlaybackItem newItem)
        {
            if (firstSongChange)
            {
                firstSongChange = false;
                return;
            }
            else if (totalTracks >= INITIAL_TRACKS_REQUEST + 1)
            {
                string newId = GetMediaItemId(newItem);
                int index = playlistMediaIds.IndexOf(newId);

                if (index == -1)
                {
                    return;
                }

                currentlyPlaying = newId;

                int indexToLoadNext = nextRemoteAttempts[0];
                int indexToLoadPrev = prevRemoteAttempts[0];

                // towards end of local tracks and more to load remote
                if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && indexToLoadNext < totalTracks - 1)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        bool success = await LoadTracks(indexToLoadNext + 1, indexToLoadNext + TRACKS_PER_REQUEST);
                        if (success && index > BUFFER_FROM_LOAD)
                        {
                            prevRemoteAttempts.RemoveAt(0);
                            int deleteUpTo = index - BUFFER_FROM_LOAD;
                            for (int i = 0; i < deleteUpTo; i++)
                            {
                                PlaybackService.removeFromQueue(playlistMediaIds.First(), localLock);
                                playlistMediaIds.RemoveAt(0);
                            }
                        }
                    }
                    );
                }
                // toward end of local tracks and no more ahead on remote, go back to beginning
                else if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && PlaybackService.queue.AutoRepeatEnabled && indexToLoadNext >= totalTracks - 1)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        bool success = await LoadTracks(0, TRACKS_PER_REQUEST - 1);
                        if (success && index > BUFFER_FROM_LOAD)
                        {
                            prevRemoteAttempts.RemoveAt(0);
                            int deleteUpTo = index - BUFFER_FROM_LOAD;
                            for (int i = 0; i < deleteUpTo; i++)
                            {
                                PlaybackService.removeFromQueue(playlistMediaIds.First(), localLock);
                                playlistMediaIds.RemoveAt(0);
                            }
                        }
                    }
                    );
                }
                // towards beginning of local tracks and more to load remote
                else if (index < BUFFER_FROM_LOAD && indexToLoadPrev != 0)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        bool success = await LoadTracksReverse(indexToLoadPrev - TRACKS_PER_REQUEST, indexToLoadPrev - 1);
                        if (success && index < BUFFER_FROM_LOAD)
                        {
                            nextRemoteAttempts.RemoveAt(0);
                            int deleteUpTo = playlistMediaIds.Count - BUFFER_FROM_LOAD - 1;
                            for (int i = playlistMediaIds.Count - 1; i > deleteUpTo; i--)
                            {
                                PlaybackService.removeFromQueue(playlistMediaIds.Last(), localLock);
                                playlistMediaIds.RemoveAt(playlistMediaIds.Count - 1);
                            }
                        }
                    }
                    );
                }
            }
        }

        /// <summary>
        /// An item failed to open
        /// </summary>
        /// <param name="item">The item that failed to open</param>
        public void ItemFailedToOpen(MediaPlaybackItem item)
        {
            PlaybackService.removeFromQueue(GetMediaItemId(item), localLock);
            playlistMediaIds.RemoveAt(playlistMediaIds.IndexOf(GetMediaItemId(item)));
            UpdateFailuresCount(1);
        }

        /// <summary>
        /// Retreive the id from the playback item
        /// </summary>
        /// <param name="item">The playback item to get the id of</param>
        /// <returns>The mediaItemId of the playback item</returns>
        private string GetMediaItemId(MediaPlaybackItem item)
        {
            if (item.Source.CustomProperties.TryGetValue("mediaItemId", out object id))
            {
                return id.ToString();
            }
            return "";
        }

        /// <summary>
        /// Gets the tracks of the playlist in a specified range
        /// </summary>
        /// <param name="start">The first track to get in the remote playlist</param>
        /// <param name="limit">The number of tracks to get after the first track in the remote playlist</param>
        /// <returns>The tracks in the specified range from the remote playlist</returns>
        private async Task<List<Track>> GetTracks(int start, int limit)
        {
            List<Track> tracks = new List<Track>();

            string trackUrl = "";
            if (type == PlaybackType.Single)
            {
                trackUrl = tracksHref;
            }
            else
            {
                UriBuilder tracksBuilder = new UriBuilder(tracksHref);
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("limit", limit.ToString()),
                    new KeyValuePair<string, string>("offset", start.ToString())
                };
                tracksBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
                trackUrl = tracksBuilder.Uri.ToString();
            }

            string tracksString = await RequestHandler.SendCliGetRequest(trackUrl);

            if (localLock != PlaybackService.globalLock) { return tracks; }

            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException) { return tracks; }

            if (localLock != PlaybackService.globalLock) { return tracks; }

            if (type == PlaybackType.Single)
            {
                Track track = new Track();
                await track.setInfoDirect(tracksJson.Stringify());
                tracks.Add(track);
                totalTracks = 1;
            }
            else
            {
                if (tracksJson.TryGetValue("items", out IJsonValue itemsJson))
                {
                    JsonArray tracksArray = itemsJson.GetArray();
                    if (tracksArray.Count > 0)
                    {
                        foreach (IJsonValue trackJson in tracksArray)
                        {
                            Track track = new Track();
                            if (type == PlaybackType.Album)
                            {
                                if (trackJson.GetObject().TryGetValue("href", out IJsonValue hrefJson))
                                {
                                    string fullTrackString = await RequestHandler.SendCliGetRequest(hrefJson.GetString());
                                    await track.setInfoDirect(fullTrackString);
                                }
                            }
                            else
                            {
                                await track.setInfo(trackJson.GetObject().Stringify());
                            }
                            tracks.Add(track);
                        }
                    }
                }
                if (tracksJson.TryGetValue("total", out IJsonValue totalJson))
                {
                    totalTracks = Convert.ToInt32(totalJson.GetNumber());
                }
            }

            return tracks;
        }

        /// <summary>
        /// Get the video id of the matching song in YouTube
        /// </summary>
        /// <param name="track">The track to search for a match in YouTube</param>
        /// <returns>The YouTube ID of the video</returns>
        private string SearchForVideoId(Track track)
        {
            YouTubeService youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = youtubeApplicationName,
                ApiKey = youtubeApiKey,
            });
            SearchResource.ListRequest listRequest = youtube.Search.List("snippet");
            listRequest.Fields = "items(id)";
            listRequest.Q = track.name + " " + track.artists[0].name + " " + track.album.name;
            listRequest.MaxResults = 1;
            listRequest.Type = "video";
            SearchListResponse resp = listRequest.Execute();
            if (resp.Items.Count == 1)
            {
                return resp.Items[0].Id.VideoId;
            }
            return "";
        }

        /// <summary>
        /// Get video ids for multiple songs in YouTube
        /// </summary>
        /// <param name="tracks">The list of tracks to search for a match in YouTube</param>
        /// <returns>A list of YouTube IDs of the videos</returns>
        private async Task<Dictionary<Track, string>> BulkSearchForVideoId(List<Track> tracks)
        {
            YouTubeService youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = youtubeApplicationName,
                ApiKey = youtubeApiKey,
            });
            BatchRequest batch = new BatchRequest(youtube);

            Dictionary<Track, string> returnIds = new Dictionary<Track, string>();

            foreach (Track track in tracks)
            {
                SearchResource.ListRequest listRequest = youtube.Search.List("snippet");
                listRequest.Q = track.name + " " + track.artists[0].name + " " + track.album.name;
                listRequest.MaxResults = 1;
                listRequest.Type = "video";
                listRequest.Fields = "items(id)";
                batch.Queue<SearchListResponse>(listRequest, (content, error, i, message) =>
                {
                    if (error != null && error.Errors.Count > 0)
                    {
                        if (error.Errors[0].Reason == "keyInvalid")
                        {
                            PlaybackService.mainPage.SetErrorMessage("Invalid YouTube Credentials. Ensure YouTube applicationName and apiKey are correct.", localLock);
                        }
                        else
                        {
                            PlaybackService.mainPage.SetErrorMessage(String.Format("{0} ({1})", error.Errors[0].Message, error.Errors[0].Reason), localLock);
                        }
                    }
                    string videoId = "";
                    if (content != null && content.Items.Count > 0)
                    {
                        videoId = content.Items[0].Id.VideoId;
                    }
                    returnIds.Add(track, videoId);
                });
            }

            await batch.ExecuteAsync();

            return returnIds;
        }

        /// <summary>
        /// Get the MediaSource of the specified youtube video, preferring audio stream to video
        /// </summary>
        /// <param name="videoId">The id of the video to get audio for</param>
        /// <returns>The audio of the track</returns>
        private async Task<MediaSource> GetAudioAsync(string videoId, string trackName)
        {
            IEnumerable<YouTubeVideo> videos = await YouTube.Default.GetAllVideosAsync(string.Format(_videoUrlFormat, videoId));
            YouTubeVideo maxAudioVideo = null;
            YouTubeVideo maxNonAudioVideo = null;
            try
            {
                for (int i = 0; i < videos.Count(); i++)
                {
                    YouTubeVideo video = videos.ElementAt(i);
                    if (video.AdaptiveKind == AdaptiveKind.Audio)
                    {
                        if (maxAudioVideo == null || video.AudioBitrate > maxAudioVideo.AudioBitrate)
                        {
                            maxAudioVideo = video;
                        }
                    }
                    else
                    {
                        if (maxNonAudioVideo == null || video.AudioBitrate > maxNonAudioVideo.AudioBitrate)
                        {
                            maxNonAudioVideo = video;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                try
                {
                    return MediaSource.CreateFromUri(new Uri(videos.ElementAt(0).GetUri()));
                }
                catch (Exception)
                {
                    return MediaSource.CreateFromUri(new Uri(""));
                }
            }
            if (maxAudioVideo != null)
            {
                return MediaSource.CreateFromUri(new Uri(maxAudioVideo.GetUri()));
            }
            else if (maxNonAudioVideo != null)
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AllowAutoRedirect = true
                };
                HttpClient client = new HttpClient(handler);

                CancellationTokenSource cancelToken = new CancellationTokenSource();
                cancelToken.Token.Register(() =>
                {
                    client.CancelPendingRequests();
                    client.Dispose();
                    PlaybackService.mainPage.HideCancelDialog(localLock);
                });

                try
                {
                    Task<HttpResponseMessage> clientTask = client.GetAsync(new Uri(maxNonAudioVideo.GetUri()), HttpCompletionOption.ResponseContentRead, cancelToken.Token);
                    Task completedTask = await Task.WhenAny(clientTask, Task.Delay(5000));
                    if (completedTask != clientTask)
                    {
                        if (App.isInBackgroundMode)
                        {
                            cancelToken.Cancel();
                            return MediaSource.CreateFromUri(new Uri(""));
                        }
                        else
                        {
                            PlaybackService.mainPage.ShowCancelDialog(localLock, cancelToken, trackName);
                            await Task.Run(() =>
                            {
                                while ((clientTask.Status == TaskStatus.WaitingForActivation ||
                                        clientTask.Status == TaskStatus.WaitingForChildrenToComplete ||
                                        clientTask.Status == TaskStatus.WaitingToRun ||
                                        clientTask.Status == TaskStatus.Running) && !cancelToken.Token.IsCancellationRequested)
                                { }
                            });
                            PlaybackService.mainPage.HideCancelDialog(localLock);
                            if (cancelToken.Token.IsCancellationRequested)
                            {
                                return MediaSource.CreateFromUri(new Uri(""));
                            }
                        }
                    }
                    HttpResponseMessage response = clientTask.Result;
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    return MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/x-flv");
                }
                catch (OperationCanceledException)
                {
                    return MediaSource.CreateFromUri(new Uri(""));
                }
            }
            return MediaSource.CreateFromUri(new Uri(""));
        }

        /// <summary>
        /// Destroy object for garbage cleanup
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
        }

        /// <summary>
        /// Implemented method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
