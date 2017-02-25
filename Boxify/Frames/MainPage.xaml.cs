using Boxify.Frames;
using System;
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

        /// <summary>
        /// The main page for the Boxify application
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to the page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
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
                safeAreaOn();
            }
            else
            {
                safeAreaOff();
            }

            SpotifyLogo.Visibility = Visibility.Collapsed;
            SpotifyLoading.Visibility = Visibility.Collapsed;
            YouTubeLogo.Visibility = Visibility.Collapsed;
            YouTubeLoading.Visibility = Visibility.Collapsed;
            YouTubeMessage.Visibility = Visibility.Collapsed;

            selectHamburgerOption(App.hamburgerOptionToLoadTo);
            updateUserUI();

            PlaybackService.mainPage = this;
            if (PlaybackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
                PlaybackMenu.Visibility = Visibility.Visible;
                if (returningFromMemoryReduction)
                {
                    await PlaybackMenu.updateUI();
                    returningFromMemoryReduction = false;
                }
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
                PlaybackMenu.Visibility = Visibility.Collapsed;
            }

            loadUserPlaylists();

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
        /// Begin loading the Users Playlists
        /// </summary>
        public async void loadUserPlaylists()
        {
            if (UserProfile.isLoggedIn() && YourMusic.playlistsSave == null)
            {
                YourMusic.refreshing = true;
                await YourMusic.setPlaylists();
                YourMusic.refreshing = false;
            }
        }

        /// <summary>
        /// Make the playback controls visible
        /// </summary>
        public async void showPlaybackMenu()
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
                PlaybackMenu.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// Set the visibility state of the PlaybackMenu progress ring
        /// </summary>
        /// <param name="visible"></param>
        public async void setPlaybackMenu(bool active)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlaybackMenu.setLoadingActive(active);
            });
        }

        /// <summary>
        /// Return the PlaybackMenu control. Needed for static PlaybackService class.
        /// </summary>
        /// <returns>The PlaybackMenu control</returns>
        public Playback getPlaybackMenu()
        {
            return PlaybackMenu;
        }

        /// <summary>
        /// Toggle the hamburger menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
        }

        /// <summary>
        /// Return to the previous frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainContentFrame.CanGoBack)
            {
                MainContentFrame.GoBack();
            }
        }

        /// <summary>
        /// Set the selected option of the hamburger navigation menu
        /// </summary>
        /// <param name="option"></param>
        public void selectHamburgerOption(string option)
        {
            HamburgerOptions.SelectedIndex = -1;
            if (option == "SettingsItem")
            {
                SettingsButton.Background = (SolidColorBrush)Resources["SystemControlHighlightListAccentLowBrush"];
                SettingsButton.Focus(FocusState.Programmatic);
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
                    item.Focus(FocusState.Programmatic);
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
                    MainContentFrame.Navigate(typeof(Browse), this);
                    Title.Text = "Browse";
                }
                else if (YourMusicItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(YourMusic), this);
                    Title.Text = "Your Music";
                }
                else if (SearchItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(Search), this);
                    Title.Text = "Search";
                }
                else if (ProfileItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(Profile), this);
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
        public void updateUserUI()
        {
            UserName.Text = UserProfile.displalyName;
            UserPic.ImageSource = UserProfile.userPic;
            if (UserProfile.displalyName == "")
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
        public void safeAreaOff()
        {
            NavLeftBorder.Visibility = Visibility.Collapsed;
            Header.Margin = new Thickness(0, 0, 0, 0);
            MainSplitView.Margin = new Thickness(0, 0, 0, 0);
            HamburgerOptions.Margin = new Thickness(0, 0, 0, 0);
            RightMainBackground.Margin = new Thickness(66, 0, 0, 0);
            if (PlaybackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
            }
            PlaybackMenu.safeAreaOff();
        }

        /// <summary>
        /// Add extra margin to ensure content inside of TV safe area
        /// </summary>
        public void safeAreaOn()
        {
            NavLeftBorder.Visibility = Visibility.Visible;
            Header.Margin = new Thickness(48, 27, 48, 0);
            MainSplitView.Margin = new Thickness(48, 0, 48, 0);
            HamburgerOptions.Margin = new Thickness(0, 0, 0, 48);
            RightMainBackground.Margin = new Thickness(114, 0, 0, 0);
            if (PlaybackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 148);
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
            }
            PlaybackMenu.safeAreaOn();
        }

        /// <summary>
        /// When a user selects any of the user information elements
        /// </summary>
        /// <param name="sender">The user element that was pressed</param>
        /// <param name="e">The pointer routed event arguments</param>
        private void userElement_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            selectHamburgerOption("ProfileItem");
        }

        /// <summary>
        /// When key is pressed, check for special key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.GamepadView)
            {
                MainSplitView.IsPaneOpen = !MainSplitView.IsPaneOpen;
            }
            else if (e.Key == VirtualKey.GamepadY)
            {
                selectHamburgerOption("SearchItem");
            }
            else if (e.Key == VirtualKey.GamepadX)
            {
                if (PlaybackService.showing)
                {
                    PlaybackMenu.focusPlayPause();
                }
            }
            else if (e.Key == VirtualKey.GamepadRightThumbstickButton)
            {
                if (PlaybackService.showing)
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
            }
            else if (e.Key == VirtualKey.GamepadRightShoulder)
            {
                if (PlaybackService.showing)
                {
                    PlaybackService.nextTrack();
                }
            }
            else if (e.Key == VirtualKey.GamepadLeftShoulder)
            {
                if (PlaybackService.showing)
                {
                    PlaybackService.previousTrack();
                }
            }
            else if (e.Key == VirtualKey.Down && e.OriginalSource is Button && ((Button)e.OriginalSource).Name == "Back")
            {
                MainContentFrame.Focus(FocusState.Programmatic);
            }
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
            selectHamburgerOption("SettingsItem");

            MainContentFrame.Navigate(typeof(Settings), this);
            Title.Text = "Settings";
        }

        /// <summary>
        /// Set whether or not the Spotify logo/loading are visible
        /// </summary>
        /// <param name="visibility">Visible to see them, Collapsed to hide them</param>
        public void bringUpSpotify()
        {
            YouTubeLogo.Visibility = Visibility.Collapsed;
            YouTubeLoading.Visibility = Visibility.Collapsed;
            YouTubeMessage.Visibility = Visibility.Collapsed;
            SpotifyLogo.Visibility = Visibility.Visible;
            SpotifyLoading.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Ensure playlist has permission to bring up Spotify info
        /// </summary>
        /// <param name="localLock"></param>
        public void bringUpSpotify(long localLock)
        {
            if (!App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                bringUpSpotify();
            }
        }

        /// <summary>
        /// Set the current Spotify loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void setSpotifyLoadingValue(double value)
        {
            SpotifyLoading.Value = value;
        }

        /// <summary>
        /// Ensure playlist has permission before setting the current Spotify loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void setSpotifyLoadingValue(double value, long localLock)
        {
            if (!App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                setSpotifyLoadingValue(value);
            }
        }

        /// <summary>
        /// Set the maximum Spotify loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void setSpotifyLoadingMaximum(double max)
        {
            SpotifyLoading.Maximum = max;
        }

        /// <summary>
        /// Ensure playlist has permission before setting the maximum Spotify loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void setSpotifyLoadingMaximum(double max, long localLock)
        {
            if (!App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                setSpotifyLoadingMaximum(max);
            }
        }

        /// <summary>
        /// Set whether or not the YouTube log/loading are visible
        /// </summary>
        /// <param name="visibility">Visible to see them, Collapsed to hide them</param>
        public void bringUpYouTube()
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
            }
        }

        /// <summary>
        /// Ensure playlist has permission to bring up YouTube info
        /// </summary>
        /// <param name="localLock"></param>
        public void bringUpYouTube(long localLock)
        {
            if (!App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                bringUpYouTube();
            }
        }

        /// <summary>
        /// Set the current YouTube loading progress
        /// </summary>
        /// <param name="value">The amount of progress made</param>
        public void setYouTubeLoadingValue(double value, long localLock)
        {
            if (YouTubeLoading != null && !App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                YouTubeLoading.Value = value;
            }
        }

        /// <summary>
        /// Set the maximum YouTube loading value
        /// </summary>
        /// <param name="max">The limit of progress</param>
        public void setYouTubeLoadingMaximum(double max, long localLock)
        {
            if (YouTubeLoading != null && !App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                YouTubeLoading.Maximum = max;
            }
        }

        /// <summary>
        /// Set the message displayed under the YouTube logo
        /// </summary>
        /// <param name="message"></param>
        public void setYouTubeMessage(String message, long localLock)
        {
            if (YouTubeMessage != null && !App.isInBackgroundMode && localLock == PlaybackService.globalLock)
            {
                YouTubeMessage.Text = message;
            }
        }

        /// <summary>
        /// Used when freeing memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                this.KeyDown -= Page_KeyDown;
                currentNavSelection = null;
            }
        }
    }
}
