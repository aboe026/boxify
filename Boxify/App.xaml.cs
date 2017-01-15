﻿using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Boxify
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        bool inBackgroundMode = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            this.EnteredBackground += App_EnteredBackground;
            this.LeavingBackground += App_LeavingBackground;

            // disable pointer
            this.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;

            // Playback Service "Initialization" of static class
            PlaybackService.queue.CurrentItemChanged += PlaybackService.songChanges;
            PlaybackService.Player.PlaybackSession.PlaybackStateChanged += PlaybackService.playStateChanges;
            PlaybackService.Player.AutoPlay = true;
        }

        /// <summary>
        /// Returning from the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            inBackgroundMode = true;
        }

        /// <summary>
        /// Going to the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            inBackgroundMode = false;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            /**
             * Needed for Xbox TV save area
             */
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

            // Set the color of the Title Bar content PC
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView"))
            {
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.InactiveBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ButtonBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ButtonInactiveBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.InactiveForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.ButtonForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.ButtonInactiveForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
            }
            
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                await loadTokenData();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Loads token information from file
        /// </summary>
        /// <returns></returns>
        private async Task loadTokenData()
        {
            string tokensString = "";
            StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
            try
            {
                StorageFile dataFile = await roamingFolder.GetFileAsync("BoxifyTokens.json");
                tokensString = await FileIO.ReadTextAsync(dataFile);
                
            }
            catch (FileNotFoundException) { }

            await RequestHandler.refreshClientCredentials();
            await RequestHandler.setTokens(tokensString, RequestHandler.SecurityFlow.AuthorizationCode);
            await RequestHandler.getClientCredentialsTokens();
        }
    }
}
