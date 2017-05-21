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

using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify.Frames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Profile : Page
    {
        private static string loggedInText = "You are currently logged in as ";
        private static string loggedOutText = "You are currently not logged in. Select the button to fix that.";

        public Profile()
        {
            this.InitializeComponent();
            MainPage.profilePage = this;
            UpdateUI();
        }

        /// <summary>
        /// Updates the UI of the frame
        /// </summary>
        private void UpdateUI()
        {
            if (UserProfile.userId == "")
            {
                WebBrowser.Visibility = Visibility.Collapsed;
                BlankUser.Text = "\uE77B";
                Status.Text = loggedOutText;
                Status.Visibility = Visibility.Visible;
                Login.Content = "Log In";
                Login.Visibility = Visibility.Visible;
            }
            else
            {
                WebBrowser.Visibility = Visibility.Collapsed;
                Status.Text = loggedInText + UserProfile.displayName;
                if (UserProfile.userPic.PixelHeight != 0)
                {
                    UserPic.ImageSource = UserProfile.userPic;
                    BlankUser.Text = "";
                }
                else
                {
                    BlankUser.Text = "\uEA8C";
                }
                Status.Visibility = Visibility.Visible;
                Login.Content = "Log Out";
                Login.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// When the webview loads a new page
        /// </summary>
        /// <param name="sender">The webview browser</param>
        /// <param name="args">The webview navigation starting event arguments</param>
        private async void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.AbsoluteUri.StartsWith(RequestHandler.callbackUrl))
            {
                WebBrowser.Visibility = Visibility.Collapsed;

                WwwFormUrlDecoder queryParams = new WwwFormUrlDecoder(args.Uri.Query);
                try
                {
                    if (queryParams.GetFirstValueByName("state") != RequestHandler.state)
                    {
                        return;
                    }
                    await RequestHandler.GetAuthorizationCodeTokens(queryParams.GetFirstValueByName("code"));
                }
                catch (ArgumentException) { return; }

                WebBrowser.Visibility = Visibility.Collapsed;
                Status.Text = loggedInText + UserProfile.displayName;
                Status.Visibility = Visibility.Visible;
                if (UserProfile.userPic.PixelHeight != 0)
                {
                    UserPic.ImageSource = UserProfile.userPic;
                }
                else
                {
                    BlankUser.Text = "\uEA8C";
                }
                UserPicContainer.Visibility = Visibility.Visible;
                Login.Content = "Log Out";
                Login.Visibility = Visibility.Visible;
                if (App.mainPage != null)
                {
                    App.mainPage.UpdateUserUI();
                }
                App.mainPage.SelectHamburgerOption("ProfileItem", true);
                App.mainPage.LoadUserPlaylists();
            }
        }

        /// <summary>
        /// Logs the user in or out
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (Login.Content.ToString() == "Log In")
            {
                Status.Visibility = Visibility.Collapsed;
                Login.Visibility = Visibility.Collapsed;
                UserPicContainer.Visibility = Visibility.Collapsed;
                BlankUser.Text = "";
                WebBrowser.Visibility = Visibility.Visible;
                WebBrowser.Focus(FocusState.Programmatic);
                WebBrowser.Navigate(RequestHandler.GetAuthorizationUri());
            }
            else if (Login.Content.ToString() == "Log Out")
            {
                UserPic.ImageSource = new BitmapImage();
                BlankUser.Text = "\uE77B";
                Status.Text = loggedOutText;
                Login.Content = "Log In";
                RequestHandler.ClearTokens();
            }
            if (App.mainPage != null)
            {
                App.mainPage.UpdateUserUI();
            }
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.isInBackgroundMode)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (WebBrowser != null)
                    {
                        WebBrowser.NavigationStarting -= WebView_NavigationStarting;
                        WebBrowser = null;
                    }
                    if (Login != null)
                    {
                        Login.Click -= Login_Click;
                        Login = null;
                    }

                    UserPic = null;
                    UserPicContainer = null;
                    BlankUser = null;
                    Status = null;
                });
            }
        }
    }
}
