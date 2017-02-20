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
            await currentSession.loadTrack(0, PlaybackSession.LoadDirection.Down);
        }

        public static void addToQueue(MediaPlaybackItem item, long localLock)
        {
            if (localLock == globalLock)
            {
                queue.Items.Add(item);
            }
        }

        public static void removeFromQueue(string mediaId, long localLock)
        {
            if (localLock == globalLock)
            {
                MediaPlaybackItem item = queue.Items.First(kvp => kvp.Source.CustomProperties["mediaItemId"].ToString() == mediaId);
                queue.Items.Remove(item);
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
        public static async void songChanges(object sender, CurrentMediaPlaybackItemChangedEventArgs e)
        {
            currentlyPlayingItem = e.NewItem;
            if (e.NewItem != null)
            {
                currentSession.songChanged(e.NewItem);
                
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (!App.isInBackgroundMode)
                    {
                        mainPage.setPlaybackMenu(false);
                        if (Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail != null)
                        {
                            IRandomAccessStreamWithContentType thumbnail = await Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail.OpenReadAsync();
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.SetSource(thumbnail);
                            mainPage.getPlaybackMenu().setTrackImage(bitmapImage);
                        }
                        mainPage.getPlaybackMenu().setTrackName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.Title);
                        mainPage.getPlaybackMenu().setAlbumName(Player.SystemMediaTransportControls.DisplayUpdater.MusicProperties.AlbumTitle);
                    }
                });
            }
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
