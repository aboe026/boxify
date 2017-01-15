using Boxify.Frames;
using System;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ListViewItem currentNavSelection = new ListViewItem();

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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            selectHamburgerOption("BrowseItem");
            updateUserUI();
            if (PlaybackService.mainPage == null)
            {
                PlaybackService.mainPage = this;
            }
            if (PlaybackService.showing)
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 100);
                PlaybackMenu.Visibility = Visibility.Visible;
            }
            else
            {
                MainContentFrame.Margin = new Thickness(0, 0, 0, 0);
                PlaybackMenu.Visibility = Visibility.Collapsed;
            }

            // settings
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["UserSettings"];
            if (composite != null)
            {
                if ((bool)composite["TvSafeAreaOff"])
                {
                    safeAreaOff();
                }
                else
                {
                    safeAreaOn();
                }

                if (composite["Theme"].ToString() == "Light")
                {
                    this.RequestedTheme = ElementTheme.Light;
                    Settings.theme = Settings.Theme.Light;
                }
                else if (composite["Theme"].ToString() == "Dark")
                {
                    this.RequestedTheme = ElementTheme.Dark;
                    Settings.theme = Settings.Theme.Dark;
                }
                else
                {
                    Settings.theme = Settings.Theme.System;
                }
            }
            else
            {
                safeAreaOn();
            }

            // load users playlists
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

            selectHamburgerOption("BrowseItem");
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
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
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
            MySplitView.IsPaneOpen = false;
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
            Settings.tvSafeArea = false;
            NavLeftBorder.Visibility = Visibility.Collapsed;
            Header.Margin = new Thickness(0, 0, 0, 0);
            MySplitView.Margin = new Thickness(0, 0, 0, 0);
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
            Settings.tvSafeArea = true;
            NavLeftBorder.Visibility = Visibility.Visible;
            Header.Margin = new Thickness(48, 27, 48, 0);
            MySplitView.Margin = new Thickness(48, 0, 48, 0);
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
                MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
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
            if (MySplitView.IsPaneOpen)
            {
                MySplitView.IsPaneOpen = false;
            }
            selectHamburgerOption("SettingsItem");

            MainContentFrame.Navigate(typeof(Settings), this);
            Title.Text = "Settings";
        }
    }
}
