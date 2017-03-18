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
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Profile : Page
    {
        private static MainPage mainPage;
        private static string loggedInText = "You are currently logged in as ";
        private static string loggedOutText = "You are currently not logged in. Select the button to fix that.";

        public Profile()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }
            UpdateUI();
        }

        /// <summary>
        /// Updates the UI of the frame
        /// </summary>
        private void UpdateUI()
        {
            if (UserProfile.DisplalyName == "")
            {
                webView.Visibility = Visibility.Collapsed;
                blankUser.Text = "\uE77B";
                status.Text = loggedOutText;
                status.Visibility = Visibility.Visible;
                login.Content = "Log In";
                login.Visibility = Visibility.Visible;
            }
            else
            {
                webView.Visibility = Visibility.Collapsed;
                status.Text = loggedInText + UserProfile.DisplalyName;
                userPic.ImageSource = UserProfile.userPic;
                blankUser.Text = "";
                status.Visibility = Visibility.Visible;
                login.Content = "Log Out";
                login.Visibility = Visibility.Visible;
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
                webView.Visibility = Visibility.Collapsed;

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

                webView.Visibility = Visibility.Collapsed;
                status.Text = loggedInText + UserProfile.DisplalyName;
                status.Visibility = Visibility.Visible;
                userPic.ImageSource = UserProfile.userPic;
                userPicContainer.Visibility = Visibility.Visible;
                login.Content = "Log Out";
                login.Visibility = Visibility.Visible;
                if (mainPage != null)
                {
                    mainPage.UpdateUserUI();
                }
                mainPage.SelectHamburgerOption("ProfileItem");
                YourMusic.playlistsSave = null;
                mainPage.LoadUserPlaylists();
            }
        }

        /// <summary>
        /// Logs the user in or out
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (login.Content.ToString() == "Log In")
            {
                status.Visibility = Visibility.Collapsed;
                login.Visibility = Visibility.Collapsed;
                userPicContainer.Visibility = Visibility.Collapsed;
                blankUser.Text = "";
                webView.Visibility = Visibility.Visible;
                webView.Focus(FocusState.Programmatic);
                webView.Navigate(RequestHandler.GetAuthorizationUri());
            }
            else if (login.Content.ToString() == "Log Out")
            {
                userPic.ImageSource = new BitmapImage();
                blankUser.Text = "\uE77B";
                status.Text = loggedOutText;
                login.Content = "Log In";
                RequestHandler.ClearTokens();
            }
            if (mainPage != null)
            {
                mainPage.UpdateUserUI();
            }
        }
    }
}
