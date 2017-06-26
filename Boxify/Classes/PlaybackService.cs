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

using Boxify.Frames;
using System;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using static Boxify.Classes.PlaybackSession;

namespace Boxify.Classes
{
    /// <summary>
    /// The central authority on playback in the application
    /// providing access to the player and active playlist.
    /// </summary>
    public class PlaybackService
    {
        public MediaPlayer Player = new MediaPlayer();
        public MediaPlaybackList queue = new MediaPlaybackList();
        public MediaPlaybackItem currentlyPlayingItem;
        
        private PlaybackSession currentSession;
        public bool showing = false;

        public long GlobalLock { get; private set; }

        /// <summary>
        /// Main constructor
        /// </summary>
        public PlaybackService()
        {
            queue.CurrentItemChanged += CurrentItemChanged;
            queue.ItemFailed += ItemFailed;
            Player.PlaybackSession.PlaybackStateChanged += PlayStateChanges;
            Player.AudioCategory = MediaPlayerAudioCategory.Media;
        }

        /// <summary>
        /// Begins playback of a new playlist of tracks
        /// </summary>
        /// <param name="type">The type of playlist (single track, album, or simple playlist)</param>
        /// <param name="href">The uri to download tracks from</param>
        public async void StartNewSession(PlaybackType type, string href, int totalTracks)
        {
            long currentLock = DateTime.Now.Ticks;
            GlobalLock = currentLock;

            if (!showing)
            {
                showing = true;
                App.mainPage.ShowPlaybackMenu();
            }
            App.mainPage.SetPlaybackMenu(true);

            if (currentSession != null)
            {
                currentSession.Dispose();
            }
            currentSession = new PlaybackSession(currentLock, Settings.playbackSource, type, Settings.shuffleEnabled, href, totalTracks);
            queue.Items.Clear();
            Player.Source = queue;
            await currentSession.LoadTracks(0, PlaybackSession.INITIAL_TRACKS_REQUEST);
            App.mainPage.SetPlaybackMenu(false);
        }

        /// <summary>
        /// Add a new track to the end of the playback queue
        /// </summary>
        /// <param name="item">The media item containing the track to add to the queue</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public void AddToQueue(MediaPlaybackItem item, long localLock)
        {
            if (localLock == GlobalLock)
            {
                queue.Items.Add(item);
            }
        }

        /// <summary>
        /// Add a new track at the beginning of the playback queue
        /// </summary>
        /// <param name="item">The media item containing the track to add to the queue</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public void AddToBeginningOfQueue(MediaPlaybackItem item, long localLock)
        {
            if (localLock == GlobalLock)
            {
                queue.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// Remove an existing track from the playback queue
        /// </summary>
        /// <param name="item">The id of the media item to remove</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public void RemoveFromQueue(string mediaId, long localLock)
        {
            if (localLock == GlobalLock)
            {
                MediaPlaybackItem item = queue.Items.First(kvp => kvp.Source.CustomProperties["mediaItemId"].ToString() == mediaId);
                queue.Items.Remove(item);
            }
        }

        /// <summary>
        /// Start playback of the queue from the first track
        /// </summary>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public void PlayFromBeginning(long localLock)
        {
            if (localLock == GlobalLock)
            {
                queue.CurrentItemChanged -= CurrentItemChanged;
                queue.MoveTo(0);
                Player.Play();
                queue.CurrentItemChanged += CurrentItemChanged;
            }
        }

        /// <summary>
        /// Move to the next track in the playlist
        /// </summary>
        public void NextTrack()
        {
            queue.MoveNext();
        }

        /// <summary>
        /// Move to the previous track in the playlist
        /// </summary>
        public void PreviousTrack()
        {
            queue.MovePrevious();
        }

        /// <summary>
        /// Toggle repeating of the playlist
        /// </summary>
        /// <returns></returns>
        public bool ToggleRepeat()
        {
            queue.AutoRepeatEnabled = !queue.AutoRepeatEnabled;
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.SetRepeatUI(queue.AutoRepeatEnabled);
            }
            else
            {
                Settings.SetRepeat(queue.AutoRepeatEnabled);
            }
            if (currentSession != null)
            {
                currentSession.ToggleRepeat(queue.AutoRepeatEnabled);
            }
            return queue.AutoRepeatEnabled;
        }

        /// <summary>
        /// Update the playback session and playback menu whether or not repeat is enabled
        /// </summary>
        /// <param name="enabled"></param>
        public void SetRepeat(bool enabled)
        {
            queue.AutoRepeatEnabled = enabled;
            if (currentSession != null)
            {
                currentSession.ToggleRepeat(enabled);
            }
            if (App.mainPage != null && App.mainPage.GetPlaybackMenu() != null)
            {
                App.mainPage.GetPlaybackMenu().SetRepeat(enabled);
            }
        }

        /// <summary>
        /// Toggle shuffling of the playlist
        /// </summary>
        /// <returns></returns>
        public bool ToggleShuffle()
        {
            if (MainPage.settingsPage != null)
            {
                MainPage.settingsPage.SetShuffleUI(!Settings.shuffleEnabled);
            }
            else
            {
                Settings.SetShuffle(!Settings.shuffleEnabled);
            }
            if (currentSession != null)
            {
                StartNewSession(currentSession.type, currentSession.tracksHref, currentSession.totalTracks);
            }
            return Settings.shuffleEnabled;
        }

        /// <summary>
        /// Update the playback session and playback menu whether or not shuffle is enabled
        /// </summary>
        /// <param name="enabled"></param>
        public void SetShuffle(bool enabled)
        {
            if (currentSession != null)
            {
                StartNewSession(currentSession.type, currentSession.tracksHref, currentSession.totalTracks);
            }
            if (App.mainPage != null && App.mainPage.GetPlaybackMenu() != null)
            {
                App.mainPage.GetPlaybackMenu().SetShuffle(enabled);
            }
        }

        /// <summary>
        /// When the current song being played changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CurrentItemChanged(object sender, CurrentMediaPlaybackItemChangedEventArgs e)
        {
            currentlyPlayingItem = e.NewItem;
            if (e.NewItem != null)
            {
                currentSession.SongChanged(e.NewItem);
                
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (!App.isInBackgroundMode)
                    {
                        if (Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail != null)
                        {
                            IRandomAccessStreamWithContentType thumbnail = await Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail.OpenReadAsync();
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);
                            App.mainPage.GetPlaybackMenu().SetTrackImage(bitmapImage);
                        }
                        App.mainPage.GetPlaybackMenu().SetTrackName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title);
                        App.mainPage.GetPlaybackMenu().SetArtistName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Artist);
                    }
                });
            }
        }

        /// <summary>
        /// An item failed to open after download
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ItemFailed(object sender, MediaPlaybackItemFailedEventArgs e)
        {
            currentSession.ItemFailedToOpen(e.Item);
        }

        /// <summary>
        /// When the play state changes (play vs pause)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PlayStateChanges(object sender, object e)
        {
            if (!App.isInBackgroundMode)
            {
                App.mainPage.GetPlaybackMenu().SetActionState(Player.PlaybackSession.PlaybackState);
            }
        }
    }
}
