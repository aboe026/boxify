using Boxify.Frames;
using System;
using Windows.ApplicationModel.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ListBoxItem currentNavSelection = new ListBoxItem();
        private bool tvSafeOn;

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

            // tv safe area
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["UserSettings"];
            if (composite != null)
            {
                object val = composite["TvSafeAreaOff"];
                if ((bool)composite["TvSafeAreaOff"])
                {
                    safeAreaOff();
                }
                else
                {
                    safeAreaOn();
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
                if (tvSafeOn)
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
            hamburgerOptions.SelectedIndex = -1;
            for (int i = 0; i < hamburgerOptions.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)hamburgerOptions.Items[i];
                if (option == item.Name)
                {
                    item.IsSelected = true;
                    hamburgerOptions.SelectedItem = item;
                    item.Focus(FocusState.Programmatic);
                    break;
                }
            }
        }

        /// <summary>
        /// User changes the page via the hamburger menu
        /// </summary>
        /// <param name="sender">The hamburger menu which was clicked</param>
        /// <param name="e">The selection changed event arguments</param>
        private void hamburgerOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MySplitView.IsPaneOpen = false;
            if (e.AddedItems.Count > 0)
            {
                ListBoxItem selectedItem = (ListBoxItem)e.AddedItems[e.AddedItems.Count - 1];
                if (currentNavSelection.Name == selectedItem.Name)
                {
                    return;
                }
                currentNavSelection = selectedItem;
                foreach (ListBoxItem item in hamburgerOptions.Items)
                {
                    if (item.Name != selectedItem.Name)
                    {
                        item.IsSelected = false;
                    }
                }
                if (BrowseItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(Browse), this);
                    title.Text = "Browse";
                }
                else if (YourMusicItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(YourMusic), this);
                    title.Text = "Your Music";
                }
                else if (SearchItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(Search), this);
                    title.Text = "Search";
                }
                else if (ProfileItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(User), this);
                    title.Text = "User";
                }
                else if (SettingsItem.IsSelected)
                {
                    MainContentFrame.Navigate(typeof(Settings), this);
                    title.Text = "Settings";
                }
            }
            else if (e.RemovedItems.Count > 0)
            {
                ListBoxItem selectedItem = (ListBoxItem)e.RemovedItems[e.RemovedItems.Count - 1];
                if (selectedItem.Name == currentNavSelection.Name)
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
            userName.Text = UserProfile.displalyName;
            userPic.ImageSource = UserProfile.userPic;
            if (UserProfile.displalyName == "")
            {
                blankUser.Text = "\uE77B";
                userPicContainer.StrokeThickness = 2;
            }
            else
            {
                userPicContainer.StrokeThickness = 0.5;
                userPic.ImageSource = UserProfile.userPic;
                blankUser.Text = "";
            }
        }

        /// <summary>
        /// Remove the extra margin so content touches display edge
        /// </summary>
        public void safeAreaOff()
        {
            tvSafeOn = false;
            NavLeftBorder.Visibility = Visibility.Collapsed;
            Header.Margin = new Thickness(0, 0, 0, 0);
            MySplitView.Margin = new Thickness(0, 0, 0, 0);
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
            tvSafeOn = true;
            NavLeftBorder.Visibility = Visibility.Visible;
            Header.Margin = new Thickness(48, 27, 48, 0);
            MySplitView.Margin = new Thickness(48, 0, 48, 0);
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
        /// When a user select any of the user information elements
        /// </summary>
        /// <param name="sender">The user element that was pressed</param>
        /// <param name="e">The pointer routed event arguments</param>
        private void userElement_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MainContentFrame.Navigate(typeof(User), this);
            title.Text = "User";
            ProfileItem.IsSelected = true;
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
    }
}
