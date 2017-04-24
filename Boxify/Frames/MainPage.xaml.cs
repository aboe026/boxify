﻿/*******************************************************************
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Boxify.Settings;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static ListViewItem currentNavSelection = new ListViewItem();
        public static bool returningFromMemoryReduction = false;
        public static string errorMessage = "";
        public static List<UserControl> announcementItems = new List<UserControl>();
        public static Browse browsePage;
        public static Profile profilePage;
        public static Search searchPage;
        public static Settings settingsPage;
        public static YourMusic yourMusicPage;

        /// <summary>
        /// The main page for the Boxify application
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            App.mainPage = this;
        }

        /// <summary>
        /// When the user navigates to the page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // announcements
            if (announcementItems.Count > 0)
            {
                ShowAnnouncements(announcementItems);
            }
            else
            {
                AnnouncementsContainer.Visibility = Visibility.Collapsed;
            }

            // settings
            if (Settings.theme == Theme.Light)
            {
                this.RequestedTheme = ElementTheme.Light;
            }
            else if (Settings.theme == Theme.Dark)
            {
                this.RequestedTheme = ElementTheme.Dark;
            }
            if (Settings.tvSafeArea)
            {
                SafeAreaOn();
            }
            else
            {
                SafeAreaOff();
            }

            CancelDialog.Visibility = Visibility.Collapsed;
            if (errorMessage != "")
            {
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = errorMessage;
            }
            else
            {
                ErrorMessage.Visibility = Visibility.Collapsed;
            }

            SpotifyLogo.Visibility = Visibility.Collapsed;
            SpotifyLoading.Visibility = Visibility.Collapsed;
            YouTubeLogo.Visibility = Visibility.Collapsed;
            YouTubeLoading.Visibility = Visibility.Collapsed;
            YouTubeMessage.Visibility = Visibility.Collapsed;

            SelectHamburgerOption(App.hamburgerOptionToLoadTo, true);
            if (App.hamburgerOptionToLoadTo == "SettingsItem")
            {
                SettingsButton_Click(null, null);
            }
            UpdateUserUI();
            
            if (App.playbackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
                PlaybackMenu.Visibility = Visibility.Visible;
                if (returningFromMemoryReduction)
                {
                    await PlaybackMenu.UpdateUI();
                    returningFromMemoryReduction = false;
                }
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
                PlaybackMenu.Visibility = Visibility.Collapsed;
            }

            LoadUserPlaylists();

            // Back button in title bar
            Frame rootFrame = Window.Current.Content as Frame;

            string myPages = "";
            foreach (PageStackEntry page in rootFrame.BackStack)
            {
                myPages += page.SourcePageType.ToString() + "\n";
            }

            if (rootFrame.CanGoBack)
            {
                // Show UI in title bar if opted-in and in-app backstack is not empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if in-app back stack is empty.
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
        }

        /// <summary>
        /// Show the user a list of announcements
        /// </summary>
        /// <param name="announcements"></param>
        public void ShowAnnouncements(List<UserControl> announcements)
        {
            announcementItems = announcements;
            Announcements.Content = announcementItems[0];
            PreviousAnnouncement.Visibility = Visibility.Collapsed;
            if (announcementItems.Count == 1)
            {
                NextAnnouncement.Visibility = Visibility.Collapsed;
            }
            else
            {
                NextAnnouncement.Visibility = Visibility.Visible;
            }
            AnnouncementsContainer.Visibility = Visibility.Visible;
            Announcements.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// Present the user with an announcement popup
        /// </summary>
        /// <param name="announcements"></param>
        /// <param name="settingsPage"></param>
        public void ShowAnnouncements(List<UserControl> announcements, Settings settingsPage)
        {
            MainPage.settingsPage = settingsPage;
            ShowAnnouncements(announcements);
        }

        /// <summary>
        /// Begin loading the Users Playlists
        /// </summary>
        public async void LoadUserPlaylists()
        {
            if (UserProfile.IsLoggedIn())
            {
                YourMusic.refreshing = true;
                await YourMusic.SetPlaylists();
                YourMusic.refreshing = false;
            }
        }

        /// <summary>
        /// Make the playback controls visible
        /// </summary>
        public async void ShowPlaybackMenu()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (Settings.tvSafeArea)
                {
                    MainContentFrame.Margin = new Thickness(0, 0, 0, 148);
                }
                else
                {
                    MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
                }

                PlaybackMenu.SetRepeat(Settings.repeatEnabled);
                PlaybackMenu.SetVolume(Settings.volume);

                PlaybackMenu.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Set the visibility state of the PlaybackMenu progress ring
        /// </summary>
        /// <param name="visible"></param>
        public async void SetPlaybackMenu(bool active)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlaybackMenu.SetLoadingActive(active);
            });
        }

        /// <summary>
        /// Return the PlaybackMenu control. Needed for static App.playbackService class.
        /// </summary>
        /// <returns>The PlaybackMenu control</returns>
        public Playback GetPlaybackMenu()
        {
            return PlaybackMenu;
        }

        /// <summary>
        /// Toggle the hamburger menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
        }

        /// <summary>
        /// Return to the previous frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentFrame.CanGoBack)
            {
                if (MainContentFrame.BackStack.Count == 0)
                {
                    SelectHamburgerOption("BrowseItem", true);
                }
                else
                {
                    PageStackEntry page = MainContentFrame.BackStack.ElementAt(MainContentFrame.BackStack.Count - 1);
                    HamburgerOptions.SelectionChanged -= HamburgerOptions_SelectionChanged;
                    if (page.SourcePageType == typeof(Browse))
                    {
                        SelectHamburgerOption("BrowseItem", false);
                        Title.Text = "Browse";
                    }
                    else if (page.SourcePageType == typeof(YourMusic))
                    {
                        SelectHamburgerOption("YourMusicItem", false);
                        Title.Text = "Your Music";
                    }
                    else if (page.SourcePageType == typeof(Profile))
                    {
                        SelectHamburgerOption("ProfileItem", false);
                        Title.Text = "Profile";
                    }
                    else if (page.SourcePageType == typeof(Search))
                    {
                        SelectHamburgerOption("SearchItem", false);
                        Title.Text = "Search";
                    }
                    else if (page.SourcePageType == typeof(Settings))
                    {
                        SelectHamburgerOption("SettingsItem", false);
                        Title.Text = "Settings";
                    }
                    HamburgerOptions.SelectionChanged += HamburgerOptions_SelectionChanged;
                }
                MainContentFrame.GoBack();
            }
        }

        /// <summary>
        /// Set the selected option of the hamburger navigation menu
        /// </summary>
        /// <param name="option"></param>
        public void SelectHamburgerOption(string option, Boolean setFocus)
        {
            HamburgerOptions.SelectedIndex = -1;
            if (option == "SettingsItem")
            {
                SettingsButton.Background = (SolidColorBrush)Resources["SystemControlHighlightListAccentLowBrush"];
                if (setFocus)
                {
                    SettingsButton.Focus(FocusState.Programmatic);
                }
                HamburgerOptions.SelectedItem = null;
                currentNavSelection = null;
            }
            else
            {
                SettingsButton.Background = new SolidColorBrush(Colors.Transparent);
            }
            for (int i = 0; i < HamburgerOptions.Items.Count; i++)
            {
                ListViewItem item = (ListViewItem)HamburgerOptions.Items[i];
                if (option == item.Name)
                {
                    item.IsSelected = true;
                    HamburgerOptions.SelectedItem = item;
                    if (setFocus)
                    {
                        item.Focus(FocusState.Programmatic);
                    }
                    break;
                }
                else
                {
                    item.IsSelected = false;
                }
            }
        }

        /// <summary>
        /// User changes the page via the hamburger menu
        /// </summary>
        /// <param name="sender">The hamburger menu which was clicked</param>
        /// <param name="e">The selection changed event arguments</param>
        private void HamburgerOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainSplitView.IsPaneOpen = false;
            if (e.AddedItems.Count > 0)
            {
                ListViewItem selectedItem = (ListViewItem)e.AddedItems[e.AddedItems.Count - 1];
                if (currentNavSelection != null && currentNavSelection.Name == selectedItem.Name)
                {
                    return;
                }
                SettingsButton.Background = new SolidColorBrush(Colors.Transparent);
                currentNavSelection = selectedItem;
                currentNavSelection.Focus(FocusState.Programmatic);
                foreach (ListViewItem item in HamburgerOptions.Items)
                {
                    if (item.Name != selectedItem.Name)
                    {
                        item.IsSelected = false;
                    }
                }
                if (BrowseItem.IsSelected)
                {
                    NavigateToPage(typeof(Browse));
                    Title.Text = "Browse";
                }
                else if (YourMusicItem.IsSelected)
                {
                    NavigateToPage(typeof(YourMusic));
                    Title.Text = "Your Music";
                }
                else if (SearchItem.IsSelected)
                {
                    NavigateToPage(typeof(Search));
                    Title.Text = "Search";
                }
                else if (ProfileItem.IsSelected)
                {
                    NavigateToPage(typeof(Profile));
                    Title.Text = "Profile";
                }
            }
            else if (e.RemovedItems.Count > 0)
            {
                ListViewItem selectedItem = (ListViewItem)e.RemovedItems[e.RemovedItems.Count - 1];
                if (currentNavSelection != null && currentNavSelection.Name == selectedItem.Name)
                {
                    selectedItem.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// Updates the UI of the user information
        /// </summary>
        public void UpdateUserUI()
        {
            UserName.Text = UserProfile.DisplalyName;
            UserPic.ImageSource = UserProfile.userPic;
            if (UserProfile.DisplalyName == "")
            {
                BlankUser.Text = "\uE77B";
                UserPicContainer.StrokeThickness = 2;
            }
            else
            {
                UserPicContainer.StrokeThickness = 0.5;
                UserPic.ImageSource = UserProfile.userPic;
                BlankUser.Text = "";
            }
        }

        /// <summary>
        /// Remove the extra margin so content touches display edge
        /// </summary>
        public void SafeAreaOff()
        {
            NavLeftBorder.Visibility = Visibility.Collapsed;
            Header.Margin = new Thickness(0, 0, 0, 0);
            MainSplitView.Margin = new Thickness(0, 0, 0, 0);
            HamburgerOptions.Margin = new Thickness(0, 0, 0, 0);
            RightMainBackground.Margin = new Thickness(66, 0, 0, 0);
            if (App.playbackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
            }
            PlaybackMenu.SafeAreaOff();
        }

        /// <summary>
        /// Add extra margin to ensure content inside of TV safe area
        /// </summary>
        public void SafeAreaOn()
        {
            NavLeftBorder.Visibility = Visibility.Visible;
            Header.Margin = new Thickness(48, 27, 48, 0);
            MainSplitView.Margin = new Thickness(48, 0, 48, 0);
            HamburgerOptions.Margin = new Thickness(0, 0, 0, 48);
            RightMainBackground.Margin = new Thickness(114, 0, 0, 0);
            if (App.playbackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 148);
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
            }
            PlaybackMenu.SafeAreaOn();
        }

        /// <summary>
        /// When a user selects any of the user information elements
        /// </summary>
        /// <param name="sender">The user element that was pressed</param>
        /// <param name="e">The pointer routed event arguments</param>
        private void UserElement_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SelectHamburgerOption("ProfileItem", true);
        }

        /// <summary>
        /// When key is pressed, check for special key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadView)
            {
                MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
            }
            else if (e.Key == VirtualKey.GamepadY)
            {
                SelectHamburgerOption("SearchItem", true);
            }
            else if (e.Key == VirtualKey.GamepadX)
            {
                if (App.playbackService.showing)
                {
                    PlaybackMenu.FocusPlayPause();
                }
            }
            else if (e.Key == VirtualKey.GamepadRightThumbstickButton)
            {
                if (App.playbackService.showing)
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
            }
            else if (e.Key == VirtualKey.GamepadRightThumbstickRight)
            {
                if (App.playbackService.showing)
                {
                    App.playbackService.NextTrack();
                }
            }
            else if (e.Key == VirtualKey.GamepadRightThumbstickLeft)
            {
                if (App.playbackService.showing)
                {
                    App.playbackService.PreviousTrack();
                }
            }
            else if (e.Key == VirtualKey.Down && e.OriginalSource is Button && ((Button)e.OriginalSource).Name == "Back")
            {
                MainContentFrame.Focus(FocusState.Programmatic);
            }
            else if (e.Key == VirtualKey.Escape && e.OriginalSource is Slider && ((Slider)e.OriginalSource).Name == "VolumeSlider")
            {
                PlaybackMenu.VolumeSlider_LostFocus(null, null);
                PlaybackMenu.FocusOnVolume();
            }
            else if (e.Key == VirtualKey.Escape)
            {
                BackButton_Click(null, null);
            }
        }

        /// <summary>
        /// Navigates to the desired page, ensuring all old BackStack references to the page are removed
        /// </summary>
        /// <param name="type"></param>
        private void NavigateToPage(Type type)
        {
            IEnumerable<PageStackEntry> duplicatePages = MainContentFrame.BackStack.Where(page => page.SourcePageType == type);
            if (duplicatePages.Count() > 0)
            {
                foreach (PageStackEntry page in duplicatePages)
                {
                    MainContentFrame.BackStack.Remove(page);
                }
            }
            MainContentFrame.Navigate(type, this);
        }

        /// <summary>
        /// User selects the Settings option
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainSplitView.IsPaneOpen)
            {
                MainSplitView.IsPaneOpen = false;
            }
            SelectHamburgerOption("SettingsItem", true);
            NavigateToPage(typeof(Settings));
            Title.Text = "Settings";
        }


        /// <summary>
        /// Set the error message displayed to the user
        /// </summary>
        /// <param name="message">The error message to be displayed to the user</param>
        public void SetErrorMessage(string message)
        {
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        /// <summary>
        /// Set the error message displayed to the user
        /// </summary>
        /// <param name="message">The error message to be displayed to the user</param>
        /// <param name="localLock">The lock to ensure no stale updates</param>
        public async void SetErrorMessage(string message, long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    SetErrorMessage(message);
                });
            }
        }

        /// <summary>
        /// Show the cancel dialog to let the user cancel the long download
        /// </summary>
        /// <param name="localLock">The lock to ensure no stale updates</param>
        /// <param name="cancelToken">The download token to cancel</param>
        public async void ShowCancelDialog(long localLock, CancellationTokenSource cancelToken, string trackName)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CancelDialog.SetCancelToken(cancelToken);
                    CancelDialog.SetTrackName(trackName);
                    CancelDialog.Visibility = Visibility.Visible;
                });
            }
        }

        /// <summary>
        /// Hide the cancel dialog
        /// </summary>
        /// <param name="localLock">The lock to ensure no stale updates</param>
        public async void HideCancelDialog(long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CancelDialog.Visibility = Visibility.Collapsed;
                });
            }
        }

        /// <summary>
        /// Set whether or not the Spotify logo/loading are visible
        /// </summary>
        /// <param name="visibility">Visible to see them, Collapsed to hide them</param>
        public void BringUpSpotify()
        {
            YouTubeLogo.Visibility = Visibility.Collapsed;
            YouTubeLoading.Visibility = Visibility.Collapsed;
            YouTubeMessage.Visibility = Visibility.Collapsed;
            SpotifyLogo.Visibility = Visibility.Visible;
            SpotifyLoading.Visibility = Visibility.Visible;
            UserName.SetValue(RelativePanel.RightOfProperty, SpotifyLoading);
        }

        /// <summary>
        /// Ensure playlist has permission to bring up Spotify info
        /// </summary>
        /// <param name="localLock"></param>
        public void BringUpSpotify(long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                BringUpSpotify();
            }
        }

        /// <summary>
        /// Set the current Spotify loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void SetSpotifyLoadingValue(double value)
        {
            SpotifyLoading.Value = value;
        }

        /// <summary>
        /// Ensure playlist has permission before setting the current Spotify loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void SetSpotifyLoadingValue(double value, long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                SetSpotifyLoadingValue(value);
            }
        }

        /// <summary>
        /// Set the maximum Spotify loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void SetSpotifyLoadingMaximum(double max)
        {
            SpotifyLoading.Maximum = max;
        }

        /// <summary>
        /// Ensure playlist has permission before setting the maximum Spotify loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void SetSpotifyLoadingMaximum(double max, long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                SetSpotifyLoadingMaximum(max);
            }
        }

        /// <summary>
        /// Set whether or not the YouTube log/loading are visible
        /// </summary>
        /// <param name="visibility">Visible to see them, Collapsed to hide them</param>
        public void BringUpYouTube()
        {
            if (SpotifyLogo != null && SpotifyLoading != null && YouTubeLogo != null && YouTubeLoading != null && YouTubeMessage != null)
            {
                SpotifyLogo.Visibility = Visibility.Collapsed;
                SpotifyLoading.Visibility = Visibility.Collapsed;
                YouTubeLogo.Visibility = Visibility.Visible;
                YouTubeLoading.Visibility = Visibility.Visible;
                if (YouTubeMessage.Visibility == Visibility.Collapsed)
                {
                    YouTubeMessage.Visibility = Visibility.Visible;
                    YouTubeMessage.Text = "";
                }
                UserName.SetValue(RelativePanel.RightOfProperty, YouTubeLoading);
            }
        }

        /// <summary>
        /// Ensure playlist has permission to bring up YouTube info
        /// </summary>
        /// <param name="localLock"></param>
        public void BringUpYouTube(long localLock)
        {
            if (!App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                BringUpYouTube();
            }
        }

        /// <summary>
        /// Set the current YouTube loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void SetYouTubeLoadingValue(double value, long localLock)
        {
            if (YouTubeLoading != null && !App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                YouTubeLoading.Value = value;
            }
        }

        /// <summary>
        /// Set the maximum YouTube loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void SetYouTubeLoadingMaximum(double max, long localLock)
        {
            if (YouTubeLoading != null && !App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                YouTubeLoading.Maximum = max;
            }
        }

        /// <summary>
        /// Set the message displayed under the YouTube logo
        /// </summary>
        /// <param name="message"></param>
        public async void SetYouTubeMessage(String message, long localLock)
        {
            if (YouTubeMessage != null && !App.isInBackgroundMode && localLock == App.playbackService.GlobalLock)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    YouTubeMessage.Text = message;
                });
            }
        }

        /// <summary>
        /// Used when freeing memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                this.KeyDown -= Page_KeyUp;
                currentNavSelection = null;

                if (browsePage != null)
                {
                    browsePage.Page_Unloaded(null, null);
                    browsePage = null;
                }
                if (yourMusicPage != null)
                {
                    yourMusicPage.Page_Unloaded(null, null);
                    yourMusicPage = null;
                }
                if (searchPage != null)
                {
                    searchPage.Page_Unloaded(null, null);
                    searchPage = null;
                }

                Bindings.StopTracking();
            }
        }

        /// <summary>
        /// User wishes to close Announcements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CloseAnnouncements_Click(object sender, RoutedEventArgs e)
        {
            AnnouncementsContainer.Visibility = Visibility.Collapsed;
            announcementItems.Clear();
        }

        /// <summary>
        /// User moves to the next announcement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NextAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = announcementItems.IndexOf(Announcements.Content as UserControl);
            if (currentIndex < announcementItems.Count - 1)
            {
                Announcements.Content = announcementItems[currentIndex + 1];
                PreviousAnnouncement.Visibility = Visibility.Visible;
                if (currentIndex + 1 == announcementItems.Count - 1)
                {
                    NextAnnouncement.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// User moves to previous announcement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PreviousAnnouncement_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = announcementItems.IndexOf(Announcements.Content as UserControl);
            if (currentIndex > 0)
            {
                Announcements.Content = announcementItems[currentIndex - 1];
                NextAnnouncement.Visibility = Visibility.Visible;
                if (currentIndex - 1 == 0)
                {
                    PreviousAnnouncement.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// User clicks button in announcement popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnnouncementsContainer_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                CloseAnnouncements_Click(null, null);
            }
            if (e.Key == VirtualKey.GamepadRightShoulder)
            {
                NextAnnouncement_Click(null, null);
            }
            else if (e.Key == VirtualKey.GamepadLeftShoulder)
            {
                PreviousAnnouncement_Click(null, null);
            }
        }
    }
}
