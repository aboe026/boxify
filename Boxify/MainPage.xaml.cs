using System.Collections.Generic;
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
        public static List<Track> queue = new List<Track>();
        /// <summary>
        /// The main page for the Boxify application
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            selectHamburgerOption("Browse");
            updateUserUI();
            
        }

        /// <summary>
        /// When the user navigates to the page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
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

        private void hamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyFrame.CanGoBack)
            {
                MyFrame.GoBack();
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
                    break;
                }
            }
        }

        /// <summary>
        /// User changes the page via the hamburger menu
        /// </summary>
        /// <param name="sender">The hamburger menu which was clicked</param>
        /// <param name="e">The selection changed event arguments</param>
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MySplitView.IsPaneOpen = false;
            if (YourMusic.IsSelected)
            {
                MyFrame.Navigate(typeof(YourMusic), this);
                title.Text = "Your Music";
            }
            else if (Browse.IsSelected)
            {
                MyFrame.Navigate(typeof(Browse), this);
                title.Text = "Browse";
            }
            else if (Profile.IsSelected)
            {
                MyFrame.Navigate(typeof(User), this);
                title.Text = "User";
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
        /// When a user select any of the user information elements
        /// </summary>
        /// <param name="sender">The user element that was pressed</param>
        /// <param name="e">The pointer routed event arguments</param>
        private void userElement_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(User), this);
            back.Visibility = Visibility.Collapsed;
            title.Text = "User";
            Profile.IsSelected = true;
        }

        public void setQueue(List<Track> tracks)
        {
            queue.Clear();
            queue.AddRange(tracks);
        }
    }
}
