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
using static Boxify.Frames.Settings;

namespace Boxify.Classes
{
    public class PlaybackSession : IDisposable
    {
        public enum LoadDirection { Up, Down };
        public enum PlaybackType { Single, Album, Playlist };

        private bool disposed = false;
        private const string _videoUrlFormat = "http://www.youtube.com/watch?v={0}";
        private static Random rng = new Random();

        public long localLock;
        private Playbacksource source = Playbacksource.Spotify;
        public PlaybackType type;
        private bool shuffling;
        public string tracksHref;
        private List<int> nextRemoteAttempts = new List<int>();
        private List<int> prevRemoteAttempts = new List<int>();
        private List<string> playlistMediaIds = new List<string>();
        private string currentlyPlaying = "";
        private List<int> shufflePositionsAvailable = new List<int>();
        public bool repeatOnShuffleMarker = false;
        public int totalTracks;
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
        /// <param name="source">The service to retrieve the song from</param>
        /// <param name="type">What kind of Spotify resource</param>
        /// <param name="shuffle">Whether or not shuffling is enabled for session playback</param>
        /// <param name="tracksHref">The reference to the download location for tracks</param>
        /// <param name="totalTracks">The total number of tracks in the playback session</param>
        public PlaybackSession(long currentLock, Playbacksource source, PlaybackType type, bool shuffle, string tracksHref, int totalTracks)
        {
            localLock = currentLock;
            this.source = source;
            this.type = type;
            this.shuffling = shuffle;
            this.tracksHref = tracksHref;
            this.totalTracks = totalTracks;
            prevRemoteAttempts.Add(0);
            if (App.mainPage != null)
            {
                App.mainPage.SetLoadersMessage("", localLock);
            }
        }

        /// <summary>
        /// Update the UI with the current number of tracks that have failed
        /// </summary>
        /// <param name="increment"></param>
        private void UpdateFailuresCount(int increment)
        {
            failuresCount += increment;
            App.mainPage.SetLoadersMessage(failuresCount + " track" + (failuresCount == 1 ? "" : "s") + " failed to match", localLock);
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
            int limit;

            if (shuffling)
            {
                if (shufflePositionsAvailable.Count == 0)
                {
                    ReFillShufflePositionsAvailable();
                }
                if (shufflePositionsAvailable.Count < end - start)
                {
                    limit = shufflePositionsAvailable.Count;
                }
                else
                {
                    limit = end - start;
                }
            }
            else
            {
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

                limit = end - start + 1;
            }

            if (source == Playbacksource.Spotify)
            {
                App.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                App.mainPage.SetSpotifyLoadingValue(0, localLock);
                App.mainPage.BringUpSpotify(localLock);
            }
            else if (source == Playbacksource.YouTube)
            {
                App.mainPage.SetYouTubeValues(0, limit, localLock);
                App.mainPage.BringUpYouTube(localLock);
            }

            if (localLock != App.playbackService.GlobalLock) { return false; }

            List<Track> tracks = new List<Track>();
            
            if (shuffling)
            {
                tracks = await GetTracksRandom(limit);
            }
            else
            {
                tracks = await GetTracksInRange(start, limit);
            }

            if (tracks.Count == totalTracks)
            {
                limit = totalTracks;
                if (source == Playbacksource.Spotify)
                {
                    App.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                }
                else if (source == Playbacksource.YouTube)
                {
                    App.mainPage.SetYouTubeValues(0, limit, localLock);
                }
            }

            if (tracks.Count != limit)
            {
                UpdateFailuresCount(limit - tracks.Count);
            }

            List<KeyValuePair<MediaSource, Track>> sources = new List<KeyValuePair<MediaSource, Track>>();

            if (source == Playbacksource.Spotify)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];
                    if (localLock == App.playbackService.GlobalLock)
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
                    App.mainPage.SetSpotifyLoadingValue(i + 1 + limit - tracks.Count, localLock);
                }
            }
            else if (source == Playbacksource.YouTube && localLock == App.playbackService.GlobalLock)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];

                    string videoId = "";
                    if (localLock == App.playbackService.GlobalLock)
                    {
                        videoId = await SearchForVideoId(track);
                    }

                    if (localLock == App.playbackService.GlobalLock)
                    {
                        if (videoId == "")
                        {
                            UpdateFailuresCount(1);
                        }
                        else
                        {
                            try
                            {
                                sources.Add(new KeyValuePair<MediaSource, Track>(await GetAudioAsync(videoId, track.name), track));
                            }
                            catch (Exception)
                            {
                                UpdateFailuresCount(1);
                            }
                        }
                    }
                    App.mainPage.SetYouTubeValues(i + 1 + limit - tracks.Count, limit, localLock);
                }
            }

            bool firstPlay = false;

            for (int i = 0; i < sources.Count; i++)
            {
                KeyValuePair<MediaSource, Track> pair = sources[i];
                if (localLock == App.playbackService.GlobalLock)
                {
                    MediaPlaybackItem playbackItem = new MediaPlaybackItem(pair.Key);
                    MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                    displayProperties.Type = MediaPlaybackType.Music;
                    displayProperties.MusicProperties.Title = pair.Value.name;
                    displayProperties.MusicProperties.AlbumTitle = pair.Value.album.name;
                    displayProperties.MusicProperties.Artist = pair.Value.GetMainArtistName();
                    if (pair.Value.album.imageUrl != "")
                    {
                        displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(pair.Value.album.imageUrl));
                    }
                    playbackItem.ApplyDisplayProperties(displayProperties);
                    pair.Key.CustomProperties["mediaItemId"] = pair.Value.id;

                    string id = GetMediaItemId(playbackItem);
                    if (!playlistMediaIds.Contains(id))
                    {
                        App.playbackService.AddToQueue(playbackItem, localLock);
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
                App.playbackService.PlayFromBeginning(localLock);
            }

            if (shuffling)
            {
                if (successes != limit && shufflePositionsAvailable.Count > 0)
                {
                    return await LoadTracks(0, limit - successes, true);
                }
            }
            else
            {
                if (successes != limit && end < totalTracks - 1)
                {
                    return await LoadTracks(start + limit, start + limit + (limit - tracks.Count), true);
                }
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
                App.mainPage.SetSpotifyLoadingMaximum(limit, localLock);
                App.mainPage.SetSpotifyLoadingValue(0, localLock);
                App.mainPage.BringUpSpotify(localLock);
            }
            else if (this.source == Playbacksource.YouTube)
            {
                App.mainPage.SetYouTubeValues(0, limit, localLock);
                App.mainPage.BringUpYouTube(localLock);
            }

            if (localLock != App.playbackService.GlobalLock) { return false; }

            List<Track> tracks = await GetTracksInRange(start, limit);
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
                        if (localLock == App.playbackService.GlobalLock)
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
                        App.mainPage.SetSpotifyLoadingValue(i + 1 + limit - tracks.Count, localLock);
                    }
                }
                else if (this.source == Playbacksource.YouTube && localLock == App.playbackService.GlobalLock)
                {
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];

                        string videoId = "";
                        if (localLock == App.playbackService.GlobalLock)
                        {
                            videoId = await SearchForVideoId(track);
                        }

                        if (localLock == App.playbackService.GlobalLock)
                        {
                            if (videoId == "")
                            {
                                UpdateFailuresCount(1);
                            }
                            else
                            {
                                try
                                {
                                    sources.Add(new KeyValuePair<MediaSource, Track>(await GetAudioAsync(videoId, track.name), track));
                                }
                                catch (Exception)
                                {
                                    UpdateFailuresCount(1);
                                }
                            }
                        }
                        App.mainPage.SetYouTubeValues(i + 1 + limit - tracks.Count, limit, localLock);
                    }
                }

                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<MediaSource, Track> pair = sources[i];
                    if (localLock == App.playbackService.GlobalLock)
                    {
                        MediaPlaybackItem playbackItem = new MediaPlaybackItem(pair.Key);
                        MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                        displayProperties.Type = MediaPlaybackType.Music;
                        displayProperties.MusicProperties.Title = pair.Value.name;
                        displayProperties.MusicProperties.AlbumTitle = pair.Value.album.name;
                        displayProperties.MusicProperties.Artist = pair.Value.GetMainArtistName();
                        if (pair.Value.album.imageUrl != "")
                        {
                            displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(pair.Value.album.imageUrl));
                        }
                        playbackItem.ApplyDisplayProperties(displayProperties);
                        pair.Key.CustomProperties["mediaItemId"] = pair.Value.id;

                        string id = GetMediaItemId(playbackItem);
                        if (!playlistMediaIds.Contains(id))
                        {
                            App.playbackService.AddToBeginningOfQueue(playbackItem, localLock);
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

                if (shuffling)
                {
                    // towards end of local tracks and more to load remote
                    if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && shufflePositionsAvailable.Count > 0)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            bool success = await LoadTracks(0, TRACKS_PER_REQUEST);
                            if (success && index > BUFFER_FROM_LOAD)
                            {
                                int deleteUpTo = index - BUFFER_FROM_LOAD;
                                for (int i = 0; i < deleteUpTo; i++)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.First(), localLock);
                                    if (playlistMediaIds.Count > 0)
                                    {
                                        playlistMediaIds.RemoveAt(0);
                                    }
                                }
                            }
                        });
                    }
                    // toward end of local tracks and no more ahead on remote, go back to beginning
                    else if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && App.playbackService.queue.AutoRepeatEnabled && shufflePositionsAvailable.Count == 0)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            bool success = await LoadTracks(0, TRACKS_PER_REQUEST - 1);
                            if (success && index > BUFFER_FROM_LOAD)
                            {
                                int deleteUpTo = index - BUFFER_FROM_LOAD;
                                for (int i = 0; i < deleteUpTo; i++)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.First(), localLock);
                                    if (playlistMediaIds.Count > 0)
                                    {
                                        playlistMediaIds.RemoveAt(0);
                                    }
                                }
                            }
                        });
                    }
                    // towards end of local tracks and no more ahead on remote and not repeating
                    else if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && !App.playbackService.queue.AutoRepeatEnabled && shufflePositionsAvailable.Count == 0)
                    {
                        repeatOnShuffleMarker = true;
                    }
                    // towards beginning of local tracks and more to load remote
                    else if (index < BUFFER_FROM_LOAD && shufflePositionsAvailable.Count < totalTracks - INITIAL_TRACKS_REQUEST && !repeatOnShuffleMarker)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            bool success = await LoadTracksReverse(0, TRACKS_PER_REQUEST);
                            if (success && index < BUFFER_FROM_LOAD)
                            {
                                int deleteUpTo = playlistMediaIds.Count - BUFFER_FROM_LOAD - 1;
                                for (int i = playlistMediaIds.Count - 1; i > deleteUpTo; i--)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.Last(), localLock);
                                    playlistMediaIds.RemoveAt(playlistMediaIds.Count - 1);
                                }
                            }
                        });
                    }
                }
                else
                {
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
                                if (prevRemoteAttempts.Count > 0)
                                {
                                    prevRemoteAttempts.RemoveAt(0);
                                }
                                int deleteUpTo = index - BUFFER_FROM_LOAD;
                                for (int i = 0; i < deleteUpTo; i++)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.First(), localLock);
                                    if (playlistMediaIds.Count > 0)
                                    {
                                        playlistMediaIds.RemoveAt(0);
                                    }
                                }
                            }
                        }
                        );
                    }
                    // toward end of local tracks and no more ahead on remote, go back to beginning
                    else if (index >= playlistMediaIds.Count - BUFFER_FROM_LOAD && App.playbackService.queue.AutoRepeatEnabled && indexToLoadNext >= totalTracks - 1)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            bool success = await LoadTracks(0, TRACKS_PER_REQUEST - 1);
                            if (success && index > BUFFER_FROM_LOAD)
                            {
                                if (prevRemoteAttempts.Count > 0)
                                {
                                    prevRemoteAttempts.RemoveAt(0);
                                }
                                int deleteUpTo = index - BUFFER_FROM_LOAD;
                                for (int i = 0; i < deleteUpTo; i++)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.First(), localLock);
                                    if (playlistMediaIds.Count > 0)
                                    {
                                        playlistMediaIds.RemoveAt(0);
                                    }
                                }
                            }
                        });
                    }
                    // towards beginning of local tracks and more to load remote
                    else if (index < BUFFER_FROM_LOAD && indexToLoadPrev != 0)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            bool success = await LoadTracksReverse(indexToLoadPrev - TRACKS_PER_REQUEST, indexToLoadPrev - 1);
                            if (success && index < BUFFER_FROM_LOAD)
                            {
                                if (nextRemoteAttempts.Count > 0)
                                {
                                    nextRemoteAttempts.RemoveAt(0);
                                }
                                int deleteUpTo = playlistMediaIds.Count - BUFFER_FROM_LOAD - 1;
                                for (int i = playlistMediaIds.Count - 1; i > deleteUpTo; i--)
                                {
                                    App.playbackService.RemoveFromQueue(playlistMediaIds.Last(), localLock);
                                    playlistMediaIds.RemoveAt(playlistMediaIds.Count - 1);
                                }
                            }
                        });
                    }
                }
            }
        }

        /// <summary>
        /// When repeat is toggled, ensure block on shuffling repeat is lifted
        /// </summary>
        /// <param name="enabled"></param>
        public void ToggleRepeat(bool enabled)
        {
            repeatOnShuffleMarker = false;
        }

        /// <summary>
        /// Refills the list of avaliable tracks in random order
        /// </summary>
        private void ReFillShufflePositionsAvailable()
        {
            shufflePositionsAvailable.Clear();
            for (int i = 0; i < totalTracks; i++)
            {
                shufflePositionsAvailable.Add(i);
            }
            Shuffle(shufflePositionsAvailable);
        }

        /// <summary>
        /// An item failed to open
        /// </summary>
        /// <param name="item">The item that failed to open</param>
        public void ItemFailedToOpen(MediaPlaybackItem item)
        {
            App.playbackService.RemoveFromQueue(GetMediaItemId(item), localLock);
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
        private async Task<List<Track>> GetTracksInRange(int start, int limit)
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
                // For some reason Spotify only has the fields parameter on the Getting a Playlists Tracks endpoint
                if (type == PlaybackType.Playlist)
                {
                    queryParams.Add(new KeyValuePair<string, string>("fields", "items(track(id,href,name,preview_url,duration_ms,album(id,name,images,artists(name)),artists(name)))"));
                }
                tracksBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
                trackUrl = tracksBuilder.Uri.ToString();
            }

            string tracksString = await RequestHandler.SendCliGetRequest(trackUrl);

            if (localLock != App.playbackService.GlobalLock) { return tracks; }

            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException) { return tracks; }

            if (localLock != App.playbackService.GlobalLock) { return tracks; }

            if (type == PlaybackType.Single)
            {
                Track track = new Track();
                await track.SetInfoDirect(tracksJson.Stringify());
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
                                    await track.SetInfoDirect(fullTrackString);
                                }
                            }
                            else
                            {
                                await track.SetInfo(trackJson.GetObject().Stringify());
                            }
                            tracks.Add(track);
                        }
                    }
                }
            }

            return tracks;
        }

        /// <summary>
        /// Gets the tracks of the playlist in random order
        /// </summary>
        /// <param name="limit">The number of random tracks to get from remote</param>
        /// <returns>The tracks in the specified positions from the remote playlist</returns>
        private async Task<List<Track>> GetTracksRandom(int limit)
        {
            List<Track> tracks = new List<Track>();

            for (int i = 0; i < limit && shufflePositionsAvailable.Count > 0; i++)
            {
                int position = shufflePositionsAvailable.ElementAt(0);
                shufflePositionsAvailable.RemoveAt(0);
                string trackUrl = "";
                UriBuilder tracksBuilder = new UriBuilder(tracksHref);
                List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("limit", "1"),
                    new KeyValuePair<string, string>("offset", position.ToString())
                };
                tracksBuilder.Query = RequestHandler.ConvertToQueryString(queryParams);
                trackUrl = tracksBuilder.Uri.ToString();

                string tracksString = await RequestHandler.SendCliGetRequest(trackUrl);

                if (localLock != App.playbackService.GlobalLock) { return tracks; }

                JsonObject tracksJson = new JsonObject();
                try
                {
                    tracksJson = JsonObject.Parse(tracksString);
                }
                catch (COMException) { return tracks; }

                if (localLock != App.playbackService.GlobalLock) { return tracks; }

                if (type == PlaybackType.Single)
                {
                    Track track = new Track();
                    await track.SetInfoDirect(tracksJson.Stringify());
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
                                        await track.SetInfoDirect(fullTrackString);
                                    }
                                }
                                else
                                {
                                    await track.SetInfo(trackJson.GetObject().Stringify());
                                }
                                tracks.Add(track);
                            }
                        }
                    }
                }
            }

            return tracks;
        }

        /// <summary>
        /// Get the video id of the matching song in YouTube
        /// </summary>
        /// <param name="track">The track to search for a match in YouTube</param>
        /// <returns>The YouTube ID of the video</returns>
        private async Task<string> SearchForVideoId(Track track)
        {
            return await RequestHandler.SearchYoutube(track.name + " " + track.artists[0].name + " " + track.album.name);
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
                    App.mainPage.HideCancelDialog(localLock);
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
                            App.mainPage.ShowCancelDialog(localLock, cancelToken, trackName);
                            await Task.Run(() =>
                            {
                                while ((clientTask.Status == TaskStatus.WaitingForActivation ||
                                        clientTask.Status == TaskStatus.WaitingForChildrenToComplete ||
                                        clientTask.Status == TaskStatus.WaitingToRun ||
                                        clientTask.Status == TaskStatus.Running) && !cancelToken.Token.IsCancellationRequested)
                                { }
                            });
                            App.mainPage.HideCancelDialog(localLock);
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
        /// Shuffles list randomly
        /// </summary>
        /// <param name="list">The list to randomly shuffle</param>
        public static void Shuffle(List<int> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Implemented method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Destroy object for garbage cleanup
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (!disposing)
            {
                playlistMediaIds.Clear();
                playlistMediaIds = null;

                prevRemoteAttempts.Clear();
                prevRemoteAttempts = null;

                nextRemoteAttempts.Clear();
                nextRemoteAttempts = null;
            }
        }
    }
}
