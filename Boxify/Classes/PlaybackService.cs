using Boxify.Classes;
using System;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using static Boxify.Classes.PlaybackSession;

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
        public static MediaPlaybackItem currentlyPlayingItem;

        public static MainPage mainPage;
        private static PlaybackSession currentSession;
        public static bool showing = false;

        public static long globalLock { get; private set; }

        /// <summary>
        /// Begins playback of a new playlist of tracks
        /// </summary>
        /// <param name="type">The type of playlist (single track, album, or simple playlist)</param>
        /// <param name="href">The uri to download tracks from</param>
        public static async void startNewSession(PlaybackType type, string href)
        {
            long currentLock = DateTime.Now.Ticks;
            globalLock = currentLock;

            if (!showing)
            {
                showing = true;
                mainPage.showPlaybackMenu();
            }
            mainPage.setPlaybackMenu(true);

            if (currentSession != null)
            {
                currentSession.Dispose();
            }
            currentSession = new PlaybackSession(currentLock, Settings.playbackSource, type, href);
            queue.Items.Clear();
            Player.Source = queue;
            await currentSession.loadTracks(0, PlaybackSession.INITIAL_TRACKS_REQUEST);
            mainPage.setPlaybackMenu(false);
        }

        /// <summary>
        /// Add a new track to the end of the playback queue
        /// </summary>
        /// <param name="item">The media item containing the track to add to the queue</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public static void addToQueue(MediaPlaybackItem item, long localLock)
        {
            if (localLock == globalLock)
            {
                queue.Items.Add(item);
            }
        }

        /// <summary>
        /// Add a new track at the beginning of the playback queue
        /// </summary>
        /// <param name="item">The media item containing the track to add to the queue</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public static void addToBeginningOfQueue(MediaPlaybackItem item, long localLock)
        {
            if (localLock == globalLock)
            {
                queue.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// Remove an existing track from the playback queue
        /// </summary>
        /// <param name="item">The id of the media item to remove</param>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public static void removeFromQueue(string mediaId, long localLock)
        {
            if (localLock == globalLock)
            {
                MediaPlaybackItem item = queue.Items.First(kvp => kvp.Source.CustomProperties["mediaItemId"].ToString() == mediaId);
                queue.Items.Remove(item);
            }
        }

        /// <summary>
        /// Start playback of the queue from the first track
        /// </summary>
        /// <param name="localLock">The timestamp of the playback session to ensure old sessions don't interfere with the current session</param>
        public static void playFromBeginning(long localLock)
        {
            if (localLock == globalLock)
            {
                queue.MoveTo(0);
                Player.Play();
            }
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
        public static async void currentItemChanged(object sender, CurrentMediaPlaybackItemChangedEventArgs e)
        {
            currentlyPlayingItem = e.NewItem;
            if (e.NewItem != null)
            {
                currentSession.songChanged(e.NewItem);
                
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (!App.isInBackgroundMode)
                    {
                        if (Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail != null)
                        {
                            IRandomAccessStreamWithContentType thumbnail = await Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail.OpenReadAsync();
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);
                            mainPage.getPlaybackMenu().setTrackImage(bitmapImage);
                        }
                        mainPage.getPlaybackMenu().setTrackName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title);
                        mainPage.getPlaybackMenu().setArtistName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Artist);
                    }
                });
            }
        }

        /// <summary>
        /// An item failed to open after download
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void itemFailed(object sender, MediaPlaybackItemFailedEventArgs e)
        {
            currentSession.itemFailedToOpen(e.Item);
        }

        /// <summary>
        /// When the play state changes (play vs pause)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void playStateChanges(object sender, object e)
        {
            if (!App.isInBackgroundMode)
            {
                mainPage.getPlaybackMenu().setActionState(Player.PlaybackSession.PlaybackState);
            }
        }
    }
}
