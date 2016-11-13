using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class User : Page
    {
        private static MainPage mainPage;
        private static string loggedInText = "You are currently logged in as ";
        private static string loggedOutText = "You are currently not logged in. Select the button to fix that.";

        public User()
        {
            this.InitializeComponent();
            if (UserProfile.displalyName != "")
            {
                this.webView.Visibility = Visibility.Collapsed;
                this.status.Text = loggedInText + UserProfile.displalyName;
                this.status.Visibility = Visibility.Visible;
                this.login.Content = "Log Out";
                this.login.Visibility = Visibility.Visible;
            }
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
                this.webView.Visibility = Visibility.Collapsed;

                WwwFormUrlDecoder queryParams = new WwwFormUrlDecoder(args.Uri.Query);
                try
                {
                    await RequestHandler.getTokens(queryParams.GetFirstValueByName("code"));
                }
                catch (ArgumentException) { return; }

                this.webView.Visibility = Visibility.Collapsed;
                this.status.Text = loggedInText + UserProfile.displalyName;
                this.status.Visibility = Visibility.Visible;
                this.login.Content = "Log Out";
                this.login.Visibility = Visibility.Visible;

                if (mainPage != null)
                {
                    mainPage.setUserName(UserProfile.displalyName);
                    mainPage.setUserPicture(UserProfile.userPic);
                }
            }
        }

        private async void login_Click(object sender, RoutedEventArgs e)
        {
            if (login.Content.ToString() == "Log In")
            {
                this.status.Visibility = Visibility.Collapsed;
                this.login.Visibility = Visibility.Collapsed;
                this.webView.Visibility = Visibility.Visible;
                this.webView.Navigate(RequestHandler.getAuthorizationUri());
            }
            else if (login.Content.ToString() == "Log Out")
            {
                this.status.Text = loggedOutText;
                this.login.Content = "Log In";
                await RequestHandler.clearTokens();
                if (mainPage != null)
                {
                    mainPage.setUserName(UserProfile.displalyName);
                    mainPage.setUserPicture(UserProfile.userPic);
                }
            }
        }
    }
}
