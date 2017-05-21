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
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls
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
            uiUpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        /// <summary>
        /// Updates the UI with the information of the currently playing track
        /// </summary>
        public async Task UpdateUI()
        {
            if (App.playbackService.currentlyPlayingItem != null)
            {
                MediaItemDisplayProperties displayProperties = App.playbackService.currentlyPlayingItem.GetDisplayProperties();
                TrackName.Text = displayProperties.MusicProperties.Title;
                TrackArtist.Text = displayProperties.MusicProperties.AlbumTitle;
                if (displayProperties.Thumbnail != null)
                {
                    IRandomAccessStreamWithContentType thumbnail = await displayProperties.Thumbnail.OpenReadAsync();
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(thumbnail);
                    AlbumArt.Source = bitmapImage;
                }
                if (App.playbackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    Play.Visibility = Visibility.Collapsed;
                    uiUpdateTimer.Start();
                }
                else
                {
                    Pause.Visibility = Visibility.Collapsed;
                }
                UpdateProgressUI();
                LoadingTrack.IsActive = false;
            }
            else
            {
                SetLoadingActive(true);
            }
        }

        /// <summary>
        /// Set the currently playing track image
        /// </summary>
        /// <param name="image">The image of the currently playing song</param>
        public void SetTrackImage(BitmapImage image)
        {
            AlbumArt.Source = image;
        }

        /// <summary>
        /// Set the currently playing track name
        /// </summary>
        /// <param name="name">The name of the currently playing song</param>
        public void SetTrackName(String name)
        {
            TrackName.Text = name;
        }

        /// <summary>
        /// Set the currently playing track album name
        /// </summary>
        /// <param name="name">The name of the album of the currently playing song</param>
        public void SetArtistName(String name)
        {
            TrackArtist.Text = name;
        }

        /// <summary>
        /// Adjust the UI according to play/pause state
        /// </summary>
        /// <param name="state">The current playing state</param>
        public async void SetActionState(MediaPlaybackState state)
        {
            if (state == MediaPlaybackState.Playing)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiUpdateTimer.Start();
                    bool manualClick = Play.FocusState == FocusState.Keyboard;
                    Play.Visibility = Visibility.Collapsed;
                    Pause.Visibility = Visibility.Visible;
                    if (manualClick)
                    {
                        Pause.Focus(FocusState.Programmatic);
                    }
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiUpdateTimer.Stop();
                    bool manualClick = Pause.FocusState == FocusState.Keyboard;
                    Play.Visibility = Visibility.Visible;
                    Pause.Visibility = Visibility.Collapsed;
                    if (manualClick)
                    {
                        Play.Focus(FocusState.Programmatic);
                    }
                });
            }
        }

        /// <summary>
        /// Remove the extra margin so content touches display edge
        /// </summary>
        public void SafeAreaOff()
        {
            MainPanel.Margin = new Thickness(0, 0, 0, 0);
            MainGrid.Height = 100;
        }

        /// <summary>
        /// Add extra margin to ensure content inside of TV safe area
        /// </summary>
        public void SafeAreaOn()
        {
            MainPanel.Margin = new Thickness(0, 0, 0, 48);
            MainGrid.Height = 148;
        }

        /// <summary>
        /// Update the UI with playback progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UiUpdateTimer_Tick(object sender, object e)
        {
            if (App.playbackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                UpdateProgressUI();
            }
        }

        /// <summary>
        /// Update the UI elements showing track playback progress
        /// </summary>
        private void UpdateProgressUI()
        {
            Progress.Maximum = App.playbackService.Player.PlaybackSession.NaturalDuration.TotalSeconds;
            Progress.Value = App.playbackService.Player.PlaybackSession.Position.TotalSeconds;

            CurrentTime.Text = App.playbackService.Player.PlaybackSession.Position.ToString(@"mm\:ss");
            Duration.Text = (App.playbackService.Player.PlaybackSession.NaturalDuration - App.playbackService.Player.PlaybackSession.Position).ToString(@"mm\:ss");
        }

        /// <summary>
        /// When user selects to play or pause the current song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (App.playbackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                App.playbackService.Player.Pause();
            }
            else
            {
                App.playbackService.Player.Play();
            }
        }
        
        /// <summary>
        /// User selects to skip to the previous song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            App.playbackService.PreviousTrack();
            if (App.playbackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MediaPlaybackItem newTrack = null;
                    while (newTrack == null)
                    {
                        newTrack = App.playbackService.queue.CurrentItem;
                    }
                    Progress.Maximum = newTrack.Source.Duration.Value.TotalSeconds;
                    Progress.Value = 0;
                    CurrentTime.Text = (TimeSpan.FromSeconds(Progress.Value)).ToString(@"mm\:ss");
                    Duration.Text = (TimeSpan.FromSeconds(Progress.Maximum)).ToString(@"mm\:ss");
                });
            }
        }

        /// <summary>
        /// User selects to skip to the next song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            App.playbackService.NextTrack();
            if (App.playbackService.Player.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MediaPlaybackItem newTrack = null;
                    while (newTrack == null)
                    {
                        newTrack = App.playbackService.queue.CurrentItem;
                    }
                    Progress.Maximum = newTrack.Source.Duration.Value.TotalSeconds;
                    Progress.Value = 0;
                    CurrentTime.Text = (TimeSpan.FromSeconds(Progress.Value)).ToString(@"mm\:ss");
                    Duration.Text = (TimeSpan.FromSeconds(Progress.Maximum)).ToString(@"mm\:ss");
                });
            }
        }

        /// <summary>
        /// User selects to toggle repeating of the playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            bool repeatOn = App.playbackService.ToggleRepeat();
            if (repeatOn)
            {
                Repeat.Visibility = Visibility.Collapsed;
                RepeatEnabled.Visibility = Visibility.Visible;
                RepeatEnabled.Focus(FocusState.Programmatic);
                Volume.SetValue(RelativePanel.AboveProperty, RepeatEnabled);
                TrackName.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                Duration.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
            }
            else
            {
                Repeat.Visibility = Visibility.Visible;
                RepeatEnabled.Visibility = Visibility.Collapsed;
                Repeat.Focus(FocusState.Programmatic);
                Volume.SetValue(RelativePanel.AboveProperty, Repeat);
                TrackName.SetValue(RelativePanel.LeftOfProperty, Repeat);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, Repeat);
                Duration.SetValue(RelativePanel.LeftOfProperty, Repeat);
            }
            Settings.repeatEnabled = repeatOn;
            Settings.SaveSettings();
        }

        /// <summary>
        /// Sets whether or not the playlist automatically repeats
        /// </summary>
        /// <param name="enabled">True to make the playlist automatically repeat, false otherwise</param>
        public void SetRepeat(bool enabled)
        {
            if (enabled)
            {
                App.playbackService.queue.AutoRepeatEnabled = true;
                Repeat.Visibility = Visibility.Collapsed;
                RepeatEnabled.Visibility = Visibility.Visible;
                RepeatEnabled.Focus(FocusState.Programmatic);
                Volume.SetValue(RelativePanel.AboveProperty, RepeatEnabled);
                TrackName.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                Duration.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
            }
            else
            {
                App.playbackService.queue.AutoRepeatEnabled = false;
                Repeat.Visibility = Visibility.Visible;
                RepeatEnabled.Visibility = Visibility.Collapsed;
                Repeat.Focus(FocusState.Programmatic);
                Volume.SetValue(RelativePanel.AboveProperty, Repeat);
                TrackName.SetValue(RelativePanel.LeftOfProperty, Repeat);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, Repeat);
                Duration.SetValue(RelativePanel.LeftOfProperty, Repeat);
            }
        }

        /// <summary>
        /// Sets the volume for playback
        /// </summary>
        /// <param name="volume">The volume, 0 to 100</param>
        public void SetVolume(double volume)
        {
            App.playbackService.Player.Volume = volume;
            VolumeSlider.Value = volume;
            if (VolumeSlider.Value == 0)
            {
                Volume.Content = "\uE74F";
            }
            else if (VolumeSlider.Value > 0 && VolumeSlider.Value <= 33)
            {
                Volume.Content = "\uE993";
            }
            else if (VolumeSlider.Value > 30 && VolumeSlider.Value <= 66)
            {
                Volume.Content = "\uE994";
            }
            else
            {
                Volume.Content = "\uE995";
            }
        }

        /// <summary>
        /// User selects to change volume
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Volume_Click(object sender, RoutedEventArgs e)
        {
            VolumeSlider.Value = App.playbackService.Player.Volume * 100;
            Volume.Visibility = Visibility.Collapsed;
            if (Settings.repeatEnabled)
            {
                RepeatEnabled.Visibility = Visibility.Collapsed;
            }
            else
            {
                Repeat.Visibility = Visibility.Collapsed;
            }
            VolumeSlider.Visibility = Visibility.Visible;
            TrackName.SetValue(RelativePanel.LeftOfProperty, VolumeSlider);
            TrackArtist.SetValue(RelativePanel.LeftOfProperty, VolumeSlider);
            Duration.SetValue(RelativePanel.LeftOfProperty, VolumeSlider);
            VolumeSlider.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// User leaves the volume slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void VolumeSlider_LostFocus(object sender, RoutedEventArgs e)
        {
            Volume.Visibility = Visibility.Visible;
            VolumeSlider.Visibility = Visibility.Collapsed;
            if (Settings.repeatEnabled)
            {
                RepeatEnabled.Visibility = Visibility.Visible;
                TrackName.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
                Duration.SetValue(RelativePanel.LeftOfProperty, RepeatEnabled);
            }
            else
            {
                Repeat.Visibility = Visibility.Visible;
                TrackName.SetValue(RelativePanel.LeftOfProperty, Repeat);
                TrackArtist.SetValue(RelativePanel.LeftOfProperty, Repeat);
                Duration.SetValue(RelativePanel.LeftOfProperty, Repeat);
            }
        }

        /// <summary>
        /// Set focus on the volume button
        /// </summary>
        public void FocusOnVolume()
        {
            Volume.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// User changes the volume level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            App.playbackService.Player.Volume = VolumeSlider.Value / 100;
            if (VolumeSlider.Value == 0)
            {
                Volume.Content = "\uE74F";
            }
            else if (VolumeSlider.Value > 0 && VolumeSlider.Value <= 33)
            {
                Volume.Content = "\uE993";
            }
            else if (VolumeSlider.Value > 30 && VolumeSlider.Value <= 66)
            {
                Volume.Content = "\uE994";
            }
            else
            {
                Volume.Content = "\uE995";
            }
            Settings.volume = VolumeSlider.Value;
            Settings.SaveSettings();
        }

        /// <summary>
        /// Move UI focus to the Play/Pause button
        /// </summary>
        public void FocusPlayPause()
        {
            if (Play.Visibility == Visibility.Visible)
            {
                Play.Focus(FocusState.Programmatic);
            }
            else if (Pause.Visibility == Visibility.Visible)
            {
                Pause.Focus(FocusState.Programmatic);
            }
        }

        /// <summary>
        /// Set the visibility of the loading progress ring
        /// </summary>
        /// <param name="visible"></param>
        public void SetLoadingActive(bool active)
        {
            LoadingTrack.IsActive = active;
            if (active)
            {
                uiUpdateTimer.Stop();
                Next.Click -= Next_Click;
                Previous.Click -= Previous_Click;
                Play.Click -= PlayPause_Click;
                Pause.Click -= PlayPause_Click;
                Play.Visibility = Visibility.Visible;
                Pause.Visibility = Visibility.Collapsed;
                TrackName.Text = "";
                TrackArtist.Text = "";
                Progress.Value = 0;
                CurrentTime.Text = "00:00";
                Duration.Text = "00:00";
                AlbumArt.Source = new BitmapImage();
            }
            else
            {
                uiUpdateTimer.Start();
                Play.Visibility = Visibility.Collapsed;
                Pause.Visibility = Visibility.Visible;
                Next.Click += Next_Click;
                Previous.Click += Previous_Click;
                Play.Click += PlayPause_Click;
                Pause.Click += PlayPause_Click;
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (uiUpdateTimer != null)
                    {
                        uiUpdateTimer.Tick -= UiUpdateTimer_Tick;
                        uiUpdateTimer = null;
                    }

                    if (Repeat != null)
                    {
                        Repeat.Click -= Repeat_Click;
                        Repeat = null;
                    }
                    if (RepeatEnabled != null)
                    {
                        RepeatEnabled.Click -= Repeat_Click;
                        RepeatEnabled = null;
                    }
                    if (Volume != null)
                    {
                        Volume.Click -= Volume_Click;
                        Volume = null;
                    }
                    if (VolumeSlider != null)
                    {
                        VolumeSlider.LostFocus -= VolumeSlider_LostFocus;
                        VolumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
                        VolumeSlider = null;
                    }
                    if (Play != null)
                    {
                        Play.Click -= PlayPause_Click;
                        Play = null;
                    }
                    if (Pause != null)
                    {
                        Pause.Click -= PlayPause_Click;
                        Pause = null;
                    }
                    if (Previous != null)
                    {
                        Previous.Click -= Previous_Click;
                        Previous = null;
                    }
                    if (Next != null)
                    {
                        Next.Click -= Next_Click;
                        Next = null;
                    }

                    LoadingTrack = null;
                    AlbumArt.Source = null;
                    AlbumArt = null;
                    TrackName = null;
                    TrackArtist = null;
                    CurrentTime = null;
                    Progress = null;
                    Duration = null;

                    MainPanel = null;
                    MainGrid = null;
                });
            }
        }
    }
}
