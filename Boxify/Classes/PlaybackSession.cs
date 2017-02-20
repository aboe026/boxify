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
using System.Threading.Tasks;
using VideoLibrary;
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
        private Dictionary<string, int> mapMediaIdToRemotePos = new Dictionary<string, int>();
        private int currentOverallPosition = -1;
        private int totalTracks;
        private bool firstSongChange = true;  // the first song change is from playing the first track. Its not an actual "change"
        private bool loadLock = false;
        private int lastLoadedFromRemote;
        private int failuresCount = 0;

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
        }

        /// <summary>
        /// Loads a single track at the specified position and adds it to the queue
        /// </summary>
        /// <param name="position">The position of the song in the remote playlist</param>
        /// <param name="direction">If fail loading the song, whether to try the next or previous song in its place</param>
        /// <returns></returns>
        public async Task<bool> loadTrack(int position, LoadDirection direction)
        {
            loadLock = true;
            lastLoadedFromRemote = position;
            bool failed = false;

            if (this.source == Playbacksource.Spotify)
            {
                PlaybackService.mainPage.setSpotifyLoadingMaximum(1, localLock);
                PlaybackService.mainPage.setSpotifyLoadingValue(0, localLock);
                PlaybackService.mainPage.bringUpSpotify(localLock);
            }
            else if (this.source == Playbacksource.YouTube)
            {
                PlaybackService.mainPage.setYouTubeLoadingMaximum(1, localLock);
                PlaybackService.mainPage.setYouTubeLoadingValue(0, localLock);
                PlaybackService.mainPage.bringUpYouTube(localLock);
            }

            Track track = await getTrack(position);
            if (track.id == null)
            {
                failed = true;
                failuresCount++;
                PlaybackService.mainPage.setYouTubeMessage(failuresCount + " track" + (failuresCount == 1 ? "" : "s") + " failed to match", localLock);
            }
            else
            {
                MediaSource source = null;
                if (this.source == Playbacksource.Spotify && track.previewUrl != "" && localLock == PlaybackService.globalLock)
                {
                    source = MediaSource.CreateFromUri(new Uri(track.previewUrl));
                }
                else if (this.source == Playbacksource.YouTube)
                {
                    string videoId = searchForVideoId(track);
                    try
                    {
                        source = await GetAudioAsync(videoId);
                    }
                    catch (Exception) {
                        failed = true;
                        failuresCount++;
                    }
                }
                else
                {
                    failed = true;
                    failuresCount++;
                }

                if (!failed)
                {
                    MediaPlaybackItem playbackItem = new MediaPlaybackItem(source);
                    MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                    displayProperties.Type = MediaPlaybackType.Music;
                    displayProperties.MusicProperties.Title = track.name;
                    displayProperties.MusicProperties.AlbumTitle = track.album.name;
                    if (track.album.images.Count > 0 && track.album.images.ElementAt(0) != null)
                    {
                        displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(track.album.imageUrl));
                    }
                    playbackItem.ApplyDisplayProperties(displayProperties);
                    source.CustomProperties["mediaItemId"] = track.id;

                    PlaybackService.addToQueue(playbackItem, localLock);
                    mapMediaIdToRemotePos.Add(getMediaItemId(playbackItem), position);
                    if (currentOverallPosition == -1)
                    {
                        currentOverallPosition = position;
                    }
                }
                if (this.source == Playbacksource.Spotify)
                {
                    PlaybackService.mainPage.setSpotifyLoadingValue(1, localLock);

                }
                else if (this.source == Playbacksource.YouTube)
                {
                    PlaybackService.mainPage.setYouTubeLoadingValue(1, localLock);
                    if (failed)
                    {
                        PlaybackService.mainPage.setYouTubeMessage(failuresCount + " track" + (failuresCount == 1 ? "" : "s") + " failed to match", localLock);
                    }
                }
            }

            if (failed && position < totalTracks || (mapMediaIdToRemotePos.Count == 1 && totalTracks > 1))
            {
                return await loadTrack(position + (direction == LoadDirection.Down ? 1 : -1), direction);
            }
            loadLock = false;
            return !failed;
        }

        /// <summary>
        /// When the queue moves to another song
        /// </summary>
        /// <param name="newItem">The new song being played</param>
        public async void songChanged(MediaPlaybackItem newItem)
        {
            if (firstSongChange)
            {
                firstSongChange = false;
                return;
            }
            else if (totalTracks > 2)
            {
                string newId = getMediaItemId(newItem);

                int newPosition;
                if (!mapMediaIdToRemotePos.TryGetValue(newId, out newPosition))
                {
                    return;
                }

                // next
                if (mapMediaIdToRemotePos.ContainsKey(newId) && newPosition == getNextPosition(currentOverallPosition) && !(currentOverallPosition == 0 && newPosition != 1))
                {
                    currentOverallPosition = newPosition;

                    int nextIndexToLoadFrom = lastLoadedFromRemote + 1;

                    // remove any playlists ahead (to avoid loading duplicates)
                    // this can happen on the second run through of the playlist
                    int max = mapMediaIdToRemotePos.Values.Max();
                    if (max >= nextIndexToLoadFrom)
                    {
                        var toRemove = mapMediaIdToRemotePos.Where(pair => pair.Value >= nextIndexToLoadFrom).Select(pair => pair.Key).ToList();
                        foreach (var key in toRemove)
                        {
                            mapMediaIdToRemotePos.Remove(key);
                        }
                    }

                    // there are more playlists to load at end of playlist
                    if (nextIndexToLoadFrom < totalTracks)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            if (! loadLock)
                            {
                                bool success = await loadTrack(nextIndexToLoadFrom, LoadDirection.Down);
                                if (success && mapMediaIdToRemotePos.Count > 3)
                                {
                                    KeyValuePair<string, int> pairToRemove = mapMediaIdToRemotePos.First(kvp => kvp.Value == mapMediaIdToRemotePos.Values.Min());
                                    mapMediaIdToRemotePos.Remove(pairToRemove.Key);
                                    PlaybackService.removeFromQueue(pairToRemove.Key, localLock);
                                }
                            }
                        }
                        );
                    }
                    // load from beginning of playlist
                    else if (PlaybackService.queue.AutoRepeatEnabled && currentOverallPosition == mapMediaIdToRemotePos.Values.Max())
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            KeyValuePair<string, int> pairToRemove = mapMediaIdToRemotePos.First(kvp => kvp.Value == mapMediaIdToRemotePos.Values.Min());
                            if (!loadLock)
                            {
                                bool success = await loadTrack(0, LoadDirection.Down);
                                if (success)
                                {
                                    mapMediaIdToRemotePos.Remove(pairToRemove.Key);
                                    PlaybackService.removeFromQueue(pairToRemove.Key, localLock);
                                }
                            }
                        }
                        );
                    }
                }

                // previous
                else if (currentOverallPosition > 0 && mapMediaIdToRemotePos.ContainsKey(newId) && newPosition == getPreviousPosition(currentOverallPosition))
                {
                    currentOverallPosition = newPosition;

                    int prevIndexToLoadFrom = mapMediaIdToRemotePos.Values.Min() - 1;

                    if (prevIndexToLoadFrom >= 0)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            if (!loadLock)
                            {
                                bool success = await loadTrack(prevIndexToLoadFrom, LoadDirection.Up);
                                if (success && mapMediaIdToRemotePos.Count > 3)
                                {
                                    KeyValuePair<string, int> pairToRemove = mapMediaIdToRemotePos.First(kvp => kvp.Value == mapMediaIdToRemotePos.Values.Max());
                                    mapMediaIdToRemotePos.Remove(pairToRemove.Key);
                                    PlaybackService.removeFromQueue(pairToRemove.Key, localLock);
                                }
                            }
                        }
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Retreive the id from the playback item
        /// </summary>
        /// <param name="item">The playback item to get the id of</param>
        /// <returns>The mediaItemId of the playback item</returns>
        private string getMediaItemId(MediaPlaybackItem item)
        {
            object id = "";
            if (item.Source.CustomProperties.TryGetValue("mediaItemId", out id))
            {
                return id.ToString();
            }
            return id.ToString();
        }

        /// <summary>
        /// Gets the next position that has already been loaded from remote
        /// </summary>
        /// <param name="position">The position to check what track comes after</param>
        /// <returns>The track after the specified position</returns>
        private int getNextPosition(int position)
        {
            // last track in playlist, next will be at beginning of locally downloaded
            if (position >= totalTracks - 1)
            {
                return mapMediaIdToRemotePos.Values.Min();
            }
            else
            {
                int closestKey = totalTracks + 1;
                foreach (int key in mapMediaIdToRemotePos.Values)
                {
                    if (key > position && key < closestKey)
                    {
                        closestKey = key;
                    }
                }
                return closestKey;
            }
        }

        /// <summary>
        /// Gets the previous position that has already been loaded from remote
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private int getPreviousPosition(int position)
        {
            int closestKey = -1;
            foreach (int key in mapMediaIdToRemotePos.Values)
            {
                if (key < position && key > closestKey)
                {
                    closestKey = key;
                }
            }
            return closestKey;
        }

        /// <summary>
        /// Gets the tracks in a playlist in a specific position
        /// </summary>
        /// <param name="position">The position in the playlist to get the track</param>
        /// <returns>The track at the specific position</returns>
        private async Task<Track> getTrack(int position)
        {
            Track track = new Track();

            string trackUrl = "";
            if (type == PlaybackType.Single)
            {
                trackUrl = tracksHref;
            }
            else
            {
                UriBuilder tracksBuilder = new UriBuilder(tracksHref);
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
                queryParams.Add(new KeyValuePair<string, string>("limit", "1"));
                queryParams.Add(new KeyValuePair<string, string>("offset", position.ToString()));
                tracksBuilder.Query = RequestHandler.convertToQueryString(queryParams);
                trackUrl = tracksBuilder.Uri.ToString();
            }

            string tracksString = await RequestHandler.sendCliGetRequest(trackUrl);

            if (localLock != PlaybackService.globalLock) { return track; }

            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return track;
            }

            if (localLock != PlaybackService.globalLock) { return track; }

            if (type == PlaybackType.Single)
            {
                await track.setInfoDirect(tracksJson.Stringify());
                totalTracks = 1;
            }
            else
            {
                IJsonValue itemsJson;
                IJsonValue totalJson;
                if (tracksJson.TryGetValue("items", out itemsJson))
                {
                    JsonArray tracksArray = itemsJson.GetArray();
                    if (tracksArray.Count > 0)
                    {
                        JsonObject trackJson = tracksArray[0].GetObject();

                        if (type == PlaybackType.Album)
                        {
                            IJsonValue hrefJson;
                            if (trackJson.TryGetValue("href", out hrefJson)) {
                                string fullTrackString = await RequestHandler.sendCliGetRequest(hrefJson.GetString());
                                await track.setInfoDirect(fullTrackString);
                            }
                        }
                        else
                        {
                            await track.setInfo(trackJson.Stringify());
                        }
                    }
                }
                if (tracksJson.TryGetValue("total", out totalJson))
                {
                    totalTracks = Convert.ToInt32(totalJson.GetNumber());
                }
            }
            
            return track;
        }

        /// <summary>
        /// Get the video id of the matching song in YouTube
        /// </summary>
        /// <param name="track">The track to search for a match in YouTube</param>
        /// <returns>The YouTube ID of the video</returns>
        private static string searchForVideoId(Track track)
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
        /// Get the MediaSource of the specified youtube video, preferring audio stream to video
        /// </summary>
        /// <param name="videoId">The id of the video to get audio for</param>
        /// <returns>The audio of the track</returns>
        private static async Task<MediaSource> GetAudioAsync(string videoId)
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
                var handler = new HttpClientHandler();
                handler.AllowAutoRedirect = true;
                HttpClient client = new HttpClient(handler);
                HttpResponseMessage response = await client.GetAsync(new Uri(maxNonAudioVideo.GetUri()), HttpCompletionOption.ResponseContentRead);
                Stream stream = await response.Content.ReadAsStreamAsync();
                return MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/x-flv");
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

            youtubeApplicationName = null;
            youtubeApiKey = null;

            localLock = -1;
            tracksHref = null;
            mapMediaIdToRemotePos = null;
            currentOverallPosition = -1;
            totalTracks = 0;
            firstSongChange = true;
            loadLock = true;
            lastLoadedFromRemote = 0;
            failuresCount = 0;
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
