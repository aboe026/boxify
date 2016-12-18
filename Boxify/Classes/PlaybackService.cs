using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace Boxify
{
    /// <summary>
    /// The central authority on playback in the application
    /// providing access to the player and active playlist.
    /// </summary>
    public static class PlaybackService
    {
        public enum State { Paused, Playing };

        public static MediaPlayer Player = new MediaPlayer();
        public static MediaPlaybackList queue = new MediaPlaybackList();
        public static MainPage mainPage;
        public static bool showing = false;

        /// <summary>
        /// Plays the desired tracks
        /// </summary>
        /// <param name="tracks">A list of desired tracks to be played</param>
        public static void playQueue(List<Track> tracks)
        {
            queue.Items.Clear();
            Player.Play();
            foreach (Track track in tracks)
            {
                if (track.previewUrl != "")
                {
                    MediaSource source = MediaSource.CreateFromUri(new Uri(track.previewUrl));
                    MediaPlaybackItem playbackItem = new MediaPlaybackItem(source);
                    MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                    displayProperties.Type = MediaPlaybackType.Music;
                    displayProperties.MusicProperties.Title = track.name;
                    displayProperties.MusicProperties.AlbumTitle = track.album.name;
                    if (track.album.images.ElementAt(0) != null)
                    {
                        displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(track.album.imageUrl));
                    }
                    playbackItem.ApplyDisplayProperties(displayProperties);
                    source.CustomProperties["mediaItemId"] = track.id;

                    queue.Items.Add(playbackItem);
                }
            }
        }

        /// <summary>
        /// Add the desired tracks to the playlist queue
        /// </summary>
        /// <param name="tracks">Add the list of tracks to the playlist</param>
        public static void addToQueue(List<Track> tracks)
        {
            foreach (Track track in tracks)
            {
                if (track.previewUrl != "")
                {
                    MediaSource source = MediaSource.CreateFromUri(new Uri(track.previewUrl));
                    MediaPlaybackItem playbackItem = new MediaPlaybackItem(source);
                    MediaItemDisplayProperties displayProperties = playbackItem.GetDisplayProperties();
                    displayProperties.Type = MediaPlaybackType.Music;
                    displayProperties.MusicProperties.Title = track.name;
                    displayProperties.MusicProperties.AlbumTitle = track.album.name;
                    if (track.album.images.ElementAt(0) != null)
                    {
                        displayProperties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(track.album.imageUrl));
                    }
                    playbackItem.ApplyDisplayProperties(displayProperties);
                    source.CustomProperties["mediaItemId"] = track.id;

                    queue.Items.Add(playbackItem);
                }
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
        public static async void songChanges(object sender, object e)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                IRandomAccessStreamWithContentType thumbnail = await Player.SystemMediaTransportControls.DisplayUpdater.Thumbnail.OpenReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(thumbnail);
                mainPage.getPlaybackMenu().setTrackImage(bitmapImage);
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
                mainPage.showPlaybackMenu();
            }
            mainPage.getPlaybackMenu().setActionState(Player.PlaybackSession.PlaybackState);
        }
    }
}
