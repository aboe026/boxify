using System;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    /// <summary>
    /// Used to control the playback of songs
    /// </summary>
    public sealed partial class Playback : UserControl
    {
        private DispatcherTimer uiUpdateTimer;

        /// <summary>
        /// Main constructor
        /// </summary>
        public Playback()
        {
            this.InitializeComponent();
            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(100); 
            uiUpdateTimer.Tick += uiUpdateTimer_Tick;
        }

        /// <summary>
        /// Set the currently playing track image
        /// </summary>
        /// <param name="image">The image of the currently playing song</param>
        public void setTrackImage(BitmapImage image)
        {
            AlbumArt.Source = image;
        }

        /// <summary>
        /// Set the currently playing track name
        /// </summary>
        /// <param name="name">The name of the currently playing song</param>
        public void setTrackName(String name)
        {
            TrackName.Text = name;
        }

        /// <summary>
        /// Set the currently playing track album name
        /// </summary>
        /// <param name="name">The name of the album of the currently playing song</param>
        public void setAlbumName(String name)
        {
            TrackAlbum.Text = name;
        }

        /// <summary>
        /// Adjust the UI according to play/pause state
        /// </summary>
        /// <param name="state">The current playing state</param>
        public async void setActionState(MediaPlaybackState state)
        {
            if (state == MediaPlaybackState.Playing)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiUpdateTimer.Start();
                    PlayPause.Content = "\uE769";
                    PlayPause.Foreground = new SolidColorBrush(Colors.Black);
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiUpdateTimer.Stop();
                    PlayPause.Content = "\uE768";
                    PlayPause.Foreground = new SolidColorBrush(Colors.Green);
                });
            }
        }

        /// <summary>
        /// Remove the extra margin so content touches display edge
        /// </summary>
        public void safeAreaOff()
        {
            MainPanel.Margin = new Thickness(0, 0, 0, 0);
            MainGrid.Height = 100;
        }

        /// <summary>
        /// Add extra margin to ensure content inside of TV safe area
        /// </summary>
        public void safeAreaOn()
        {
            MainPanel.Margin = new Thickness(0, 0, 0, 48);
            MainGrid.Height = 148;
        }

        /// <summary>
        /// Update the UI with playback progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void uiUpdateTimer_Tick(object sender, object e)
        {
            if (PlaybackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                Progress.Maximum = PlaybackService.Player.PlaybackSession.NaturalDuration.TotalSeconds;
                Progress.Value = PlaybackService.Player.PlaybackSession.Position.TotalSeconds;

                CurrentTime.Text = PlaybackService.Player.PlaybackSession.Position.ToString(@"mm\:ss");
                Duration.Text = PlaybackService.Player.PlaybackSession.NaturalDuration.ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// When user selects to play or pause the current song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                PlaybackService.Player.Pause();
            }
            else
            {
                PlaybackService.Player.Play();
            }
        }
        
        /// <summary>
        /// User selects to skip to the previous song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            PlaybackService.previousTrack();
        }

        /// <summary>
        /// User selects to skip to the next song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            PlaybackService.nextTrack();
        }

        /// <summary>
        /// User selects to toggle shuffling of the playback playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            var shuffleEnabled = PlaybackService.toggleShuffle();
            if (shuffleEnabled)
            {
                Shuffle.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Shuffle.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// User selects to toggle repeating of the playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            var repeatEnabled = PlaybackService.toggleRepeat();
            if (repeatEnabled)
            {
                Repeat.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Repeat.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
    }
}
