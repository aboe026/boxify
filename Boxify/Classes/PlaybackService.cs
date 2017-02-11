using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VideoLibrary;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using static Boxify.Settings;

namespace Boxify
{
    /// <summary>
    /// The central authority on playback in the application
    /// providing access to the player and active playlist.
    /// </summary>
    public static class PlaybackService
    {
        public static MediaPlayer Player = new MediaPlayer();
        public static MediaPlaybackList queue = new MediaPlaybackList();
        public static MainPage mainPage;
        public static bool showing = false;
        public static TimeSpan currentPlaybackAttempt;
        private static int failuresCount;
        public static string youtubeApplicationName = "";
        public static string youtubeApiKey = "";
        private const string _videoUrlFormat = "http://www.youtube.com/watch?v={0}";

        /// <summary>
        /// Plays the desired tracks
        /// </summary>
        /// <param name="tracks">A list of desired tracks to be played</param>
        public static async Task<TimeSpan> playTrack(Track track, int total, Playbacksource playbackType)
        {
            TimeSpan localPlaybackAttempt = new TimeSpan(DateTime.Now.Ticks);
            currentPlaybackAttempt = localPlaybackAttempt;
            failuresCount = 0;
            queue.Items.Clear();
            Player.Source = queue;
            if (playbackType == Playbacksource.Spotify)
            {
                mainPage.setSpotifyLoadingMaximum(total);
                mainPage.setSpotifyLoadingValue(0);
                mainPage.bringUpSpotify();
            }
            else if (playbackType == Playbacksource.YouTube)
            {
                mainPage.setYouTubeLoadingMaximum(total);
                mainPage.setYouTubeLoadingValue(0);
                mainPage.bringUpYouTube();
            }

            MediaSource source = null;
            bool validTrack = false;
            if (playbackType == Playbacksource.Spotify)
            {
                if (track.previewUrl != "")
                {
                    source = MediaSource.CreateFromUri(new Uri(track.previewUrl));
                    validTrack = true;
                }
            }
            else if (playbackType == Playbacksource.YouTube)
            {
                string videoId = searchForVideoId(track);
                try
                {
                    source = await GetAudioAsync(videoId);
                    validTrack = true;
                }
                catch (UriFormatException) { failuresCount++; }
                catch (KeyNotFoundException) { failuresCount++; }
                catch (InvalidOperationException) { failuresCount++; }
            }

            if (validTrack)
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

                queue.Items.Add(playbackItem);
            }
            if (playbackType == Playbacksource.Spotify)
            {
                mainPage.setSpotifyLoadingValue(1);

            }
            else if (playbackType == Playbacksource.YouTube)
            {
                mainPage.setYouTubeLoadingValue(1);
                if (failuresCount == 1)
                {
                    mainPage.setYouTubeMessage(failuresCount.ToString() + " track failed to match");
                }
                else
                {
                    mainPage.setYouTubeMessage("");
                }
            }
            return localPlaybackAttempt;
        }

        /// <summary>
        /// Add the desired tracks to the playlist queue
        /// </summary>
        /// <param name="tracks">Add the list of tracks to the playlist</param>
        public static async Task<bool> addToQueue(List<Track> tracks, int total, int offset, TimeSpan localPlaybackAttempt, Playbacksource playbackType)
        {
            if (localPlaybackAttempt.Ticks != currentPlaybackAttempt.Ticks)
            {
                return false;
            }
            if (playbackType == Playbacksource.Spotify)
            {
                mainPage.setSpotifyLoadingMaximum(total);
                mainPage.setSpotifyLoadingValue(offset);
                mainPage.bringUpSpotify();
            }
            else if (playbackType == Playbacksource.YouTube)
            {
                mainPage.setYouTubeLoadingMaximum(total);
                mainPage.setYouTubeLoadingValue(offset);
                mainPage.bringUpYouTube();
            }

            if (playbackType == Playbacksource.Spotify)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (localPlaybackAttempt.Ticks != currentPlaybackAttempt.Ticks)
                    {
                        return false;
                    }
                    Track track = tracks.ElementAt(i);
                    MediaSource source = null;
                    bool validTrack = false;
                    if (track.previewUrl != "")
                    {
                        source = MediaSource.CreateFromUri(new Uri(track.previewUrl));
                        validTrack = true;
                    }

                    if (validTrack)
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

                        if (localPlaybackAttempt.Ticks == currentPlaybackAttempt.Ticks)
                        {
                            queue.Items.Add(playbackItem);
                        }
                    }
                    else
                    {
                        failuresCount++;
                    }
                    if (localPlaybackAttempt.Ticks == currentPlaybackAttempt.Ticks)
                    {
                        if (failuresCount == 1)
                        {
                            mainPage.setYouTubeMessage(failuresCount.ToString() + " track failed to match");
                        }
                        else if (failuresCount > 1)
                        {
                            mainPage.setYouTubeMessage(failuresCount.ToString() + " tracks failed to match");
                        }
                        mainPage.setSpotifyLoadingValue(i + offset);
                    }
                }
            }
            else if (playbackType == Playbacksource.YouTube)
            {
                List<string> videoIds = await bulkSearchForVideoId(tracks);
                int previousFailuesCount = failuresCount;
                failuresCount += tracks.Count - videoIds.Count;
                int countOffset = tracks.Count - videoIds.Count;
                if (failuresCount != previousFailuesCount && failuresCount == 1)
                {
                    mainPage.setYouTubeMessage(failuresCount.ToString() + " track failed to match");
                }
                else if (failuresCount != previousFailuesCount && failuresCount > 1)
                {
                    mainPage.setYouTubeMessage(failuresCount.ToString() + " tracks failed to match");
                }
                for (int i = 0; i < videoIds.Count; i++)
                {
                    if (localPlaybackAttempt.Ticks != currentPlaybackAttempt.Ticks)
                    {
                        return false;
                    }
                    string videoId = videoIds.ElementAt(i);
                    Track track = tracks.ElementAt(i);
                    bool validTrack = false;
                    MediaSource source = null;

                    try
                    {
                        source = await GetAudioAsync(videoId);
                        validTrack = true;
                    }
                    catch (UriFormatException) { failuresCount++; }
                    catch (KeyNotFoundException) { failuresCount++; }
                    catch (InvalidOperationException) { failuresCount++; }

                    if (validTrack)
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

                        if (localPlaybackAttempt.Ticks == currentPlaybackAttempt.Ticks)
                        {
                            queue.Items.Add(playbackItem);
                        }
                    }
                    if (localPlaybackAttempt.Ticks == currentPlaybackAttempt.Ticks)
                    {
                        if (failuresCount == 1)
                        {
                            mainPage.setYouTubeMessage(failuresCount.ToString() + " track failed to match");
                        }
                        else if (failuresCount > 1)
                        {
                            mainPage.setYouTubeMessage(failuresCount.ToString() + " tracks failed to match");
                        }
                        mainPage.setYouTubeLoadingValue(i + 1 + offset + countOffset);
                    }
                }
            }
            return localPlaybackAttempt.Ticks == currentPlaybackAttempt.Ticks;
        }

        /// <summary>
        /// Move to the next track in the playlist
        /// </summary>
        public static void nextTrack()
        {
            queue.MoveNext();
        }

        /// <summary>
        /// Move to the previous track in the playlist
        /// </summary>
        public static void previousTrack()
        {
            queue.MovePrevious();
        }

        /// <summary>
        /// Toggle shuffling of the playlist
        /// </summary>
        /// <returns></returns>
        public static bool toggleShuffle()
        {
            queue.ShuffleEnabled = !queue.ShuffleEnabled;
            return queue.ShuffleEnabled;
        }

        /// <summary>
        /// Toggle repeating of the playlist
        /// </summary>
        /// <returns></returns>
        public static bool toggleRepeat()
        {
            queue.AutoRepeatEnabled = !queue.AutoRepeatEnabled;
            return queue.AutoRepeatEnabled;
        }

        /// <summary>
        /// When the current song being played changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static async void songChanges(object sender, object e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail != null)
                {
                    IRandomAccessStreamWithContentType thumbnail = await Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail.OpenReadAsync();
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(thumbnail);
                    mainPage.getPlaybackMenu().setTrackImage(bitmapImage);
                }
                mainPage.getPlaybackMenu().setTrackName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title);
                mainPage.getPlaybackMenu().setAlbumName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.AlbumTitle);
            });
        }

        /// <summary>
        /// When the play state changes (play vs pause)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void playStateChanges(object sender, object e)
        {
            if (!showing)
            {
                showing = true;
                mainPage.showPlaybackMenu();
            }
            mainPage.getPlaybackMenu().setActionState(Player.PlaybackSession.PlaybackState);
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
        /// <returns>A list of YouTube IDs of teh videos</returns>
        private async static Task<List<string>> bulkSearchForVideoId(List<Track> tracks)
        {
            YouTubeService youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = youtubeApplicationName,
                ApiKey = youtubeApiKey,
            });
            BatchRequest batch = new BatchRequest(youtube);

            List<string> returnIds = new List<string>();

            foreach (Track track in tracks)
            {
                SearchResource.ListRequest listRequest = youtube.Search.List("snippet");
                listRequest.Q = track.name + " " + track.artists[0].name + " " + track.album.name;
                listRequest.MaxResults = 1;
                listRequest.Type = "video";
                batch.Queue<SearchListResponse>(listRequest, (content, error, i, message) =>
                {
                    if (content.Items.Count > 0)
                    {
                        string videoId = content.Items[0].Id.VideoId;
                        returnIds.Add(videoId);
                    }
                });
            }

            await batch.ExecuteAsync();

            return returnIds;
        }

        /// <summary>
        /// Get the MediaSource of the specified youtube video, preferring audio stream to video
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        private static async Task<MediaSource> GetAudioAsync(string videoId)
        {
            IEnumerable<YouTubeVideo> videos = await YouTube.Default.GetAllVideosAsync(string.Format(_videoUrlFormat, videoId));
            int maxAudioBitrate = 0;
            int maxNonAudioBitrate = 0;
            YouTubeVideo maxAudioVideo = null;
            YouTubeVideo maxNonAudioVideo = null;
            try
            {
                for (int i = 0; i < videos.Count(); i++)
                {
                    YouTubeVideo video = videos.ElementAt(i);
                    if (video.AdaptiveKind == AdaptiveKind.Audio)
                    {
                        if (video.AudioBitrate > maxAudioBitrate)
                        {
                            maxAudioBitrate = video.AudioBitrate;
                            maxAudioVideo = video;
                        }
                    }
                    else
                    {
                        if (video.AudioBitrate > maxNonAudioBitrate)
                        {
                            maxNonAudioBitrate = video.AudioBitrate;
                            maxNonAudioVideo = video;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                try
                {
                    return MediaSource.CreateFromUri(new Uri(await videos.ElementAt(0).GetUriAsync()));
                }
                catch (Exception)
                {
                    return MediaSource.CreateFromUri(new Uri(""));
                }
            }
            if (maxAudioVideo != null)
            {
                return MediaSource.CreateFromUri(new Uri(await maxAudioVideo.GetUriAsync()));
            }
            else if (maxNonAudioVideo != null)
            {
                var handler = new HttpClientHandler();
                handler.AllowAutoRedirect = true;
                HttpClient client = new HttpClient(handler);
                HttpResponseMessage response = await client.GetAsync(new Uri(await maxNonAudioVideo.GetUriAsync()), HttpCompletionOption.ResponseContentRead);
                Stream stream = await response.Content.ReadAsStreamAsync();
                return MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/x-flv");
            }
            return MediaSource.CreateFromUri(new Uri(""));
        }
    }
}
