using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Boxify.Settings;

namespace Boxify
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static bool isInBackgroundMode = false;
        private static bool finishedInitialization = false;
        public static string hamburgerOptionToLoadTo = "BrowseItem";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            // disable pointer
            this.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;

            // Playback Service "Initialization" of static class
            PlaybackService.queue.CurrentItemChanged += PlaybackService.currentItemChanged;
            PlaybackService.queue.ItemFailed += PlaybackService.itemFailed;
            PlaybackService.Player.PlaybackSession.PlaybackStateChanged += PlaybackService.playStateChanges;

            // Subscribe to key lifecyle events to know when the app
            // transitions to and from foreground and background.
            // Leaving the background is an important transition
            // because the app may need to restore UI.
            this.EnteredBackground += App_EnteredBackground;
            this.LeavingBackground += App_LeavingBackground;

            // During the transition from foreground to background the
            // memory limit allowed for the application changes. The application
            // has a short time to respond by bringing its memory usage
            // under the new limit.
            MemoryManager.AppMemoryUsageLimitChanging += MemoryManager_AppMemoryUsageLimitChanging;

            // After an application is backgrounded it is expected to stay
            // under a memory target to maintain priority to keep running.
            // Subscribe to the event that informs the app of this change.
            MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;
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
                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.InactiveBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ButtonBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ButtonInactiveBackgroundColor = ((SolidColorBrush)Resources["AppTitleBarBackground"]).Color;
                titleBar.ForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.InactiveForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.ButtonForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
                titleBar.ButtonInactiveForegroundColor = ((SolidColorBrush)Resources["AppTitleBarForeground"]).Color;
            }

            // settings
            LoadSettings();

            // load tokens
            await RequestHandler.InitializeTokens();

            // Create Frames
            CreateRootFrame(e.PreviousExecutionState, e.Arguments);

            // Ensure the current window is active
            Window.Current.Activate();

            finishedInitialization = true;
        }

        /// <summary>
        /// Gets user settings to be applied to app
        /// </summary>
        private void LoadSettings()
        {
            // settings
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["UserSettings"];
            if (composite != null)
            {
                // tv safe area
                if (composite["TvSafeAreaOff"] != null && composite["TvSafeAreaOff"].ToString() == "True")
                {
                    Settings.tvSafeArea = false;
                }
                else
                {
                    Settings.tvSafeArea = true;
                }

                // theme
                if (composite["Theme"] != null && composite["Theme"].ToString() == "Light")
                {
                    Settings.theme = Settings.Theme.Light;
                }
                else if (composite["Theme"].ToString() == "Dark")
                {
                    Settings.theme = Settings.Theme.Dark;
                }
                else
                {
                    Settings.theme = Settings.Theme.System;
                }

                // playback source
                if (composite["PlaybackSource"] != null && composite["PlaybackSource"].ToString() == "YouTube")
                {
                    Settings.playbackSource = Playbacksource.YouTube;
                }
                else
                {
                    Settings.playbackSource = Playbacksource.Spotify;
                }

                // repeat
                if (composite["RepeatEnabled"] != null && composite["RepeatEnabled"].ToString() == "True")
                {
                    Settings.repeatEnabled = true;
                }
                else
                {
                    Settings.repeatEnabled = false;
                }

                // volume
                if (composite["Volume"] != null)
                {
                    if (Double.TryParse(composite["Volume"].ToString(), out double value))
                    {
                        Settings.volume = value;
                    }
                }
                else
                {
                    Settings.volume = 100;
                }
            }
            else
            {
                // Defaults
                Settings.tvSafeArea = true;
                Settings.theme = Theme.System;
                Settings.playbackSource = Playbacksource.Spotify;
            }
        }

        /// <summary>
        /// Creates the root application page
        /// </summary>
        /// <param name="previousExecutionState"></param>
        /// <param name="arguments"></param>
        private void CreateRootFrame(ApplicationExecutionState previousExecutionState, string arguments)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame()
                {

                    // Set the default language
                    Language = Windows.Globalization.ApplicationLanguages.Languages[0]
                };
                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), arguments);
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
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Going to the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            isInBackgroundMode = true;

            PlaybackService.Player.PlaybackSession.PlaybackStateChanged -= PlaybackService.playStateChanges;

            if (PlaybackService.showing)
            {
                hamburgerOptionToLoadTo = MainPage.currentNavSelection.Name;
                ReduceMemoryUsage(MemoryManager.AppMemoryUsage);
            }
        }

        /// <summary>
        /// Returning from the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            isInBackgroundMode = false;

            // Restore view content if it was previously unloaded
            if (finishedInitialization && Window.Current.Content == null)
            {
                MainPage.returningFromMemoryReduction = true;
                CreateRootFrame(ApplicationExecutionState.Running, string.Empty);
                
                PlaybackService.Player.PlaybackSession.PlaybackStateChanged += PlaybackService.playStateChanges;
            }
        }

        /// <summary>
        /// Raised when the memory limit for the app is changing, such as when the app
        /// enters the background.
        /// </summary>
        /// <remarks>
        /// If the app is using more than the new limit, it must reduce memory within 2 seconds
        /// on some platforms in order to avoid being suspended or terminated.
        ///
        /// While some platforms will allow the application
        /// to continue running over the limit, reducing usage in the time
        /// allotted will enable the best experience across the broadest range of devices.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemoryManager_AppMemoryUsageLimitChanging(object sender, AppMemoryUsageLimitChangingEventArgs e)
        {
            // If app memory usage is over the limit, reduce usage within 2 seconds
            // so that the system does not suspend the app
            if (MemoryManager.AppMemoryUsage >= e.NewLimit)
            {
                ReduceMemoryUsage(e.NewLimit);
            }
        }

        /// <summary>
        /// Handle system notifications that the app has increased its
        /// memory usage level compared to its current target.
        /// </summary>
        /// <remarks>
        /// The app may have increased its usage or the app may have moved
        /// to the background and the system lowered the target for the app
        /// In either case, if the application wants to maintain its priority
        /// to avoid being suspended before other apps, it may need to reduce
        /// its memory usage.
        ///
        /// This is not a replacement for handling AppMemoryUsageLimitChanging
        /// which is critical to ensure the app immediately gets below the new
        /// limit. However, once the app is allowed to continue running and
        /// policy is applied, some apps may wish to continue monitoring
        /// usage to ensure they remain below the limit.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemoryManager_AppMemoryUsageIncreased(object sender, object e)
        {
            // Obtain the current usage level
            AppMemoryUsageLevel level = MemoryManager.AppMemoryUsageLevel;

            // Check the usage level to determine whether reducing memory is necessary.
            // Memory usage may have been fine when initially entering the background but
            // the app may have increased its memory usage since then and will need to trim back.
            if (level == AppMemoryUsageLevel.OverLimit || level == AppMemoryUsageLevel.High)
            {
                ReduceMemoryUsage(MemoryManager.AppMemoryUsageLimit);
            }
        }

        /// <summary>
        /// Reduces application memory usage.
        /// </summary>
        /// <remarks>
        /// When the app enters the background, receives a memory limit changing
        /// event, or receives a memory usage increased event, it can
        /// can optionally unload cached data or even its view content in
        /// order to reduce memory usage and the chance of being suspended.
        ///
        /// This must be called from multiple event handlers because an application may already
        /// be in a high memory usage state when entering the background, or it
        /// may be in a low memory usage state with no need to unload resources yet
        /// and only enter a higher state later.
        /// </remarks>
        public void ReduceMemoryUsage(ulong limit)
        {
            // If the app has caches or other memory it can free, it should do so now.
            

            // Additionally, if the application is currently
            // in background mode and still has a view with content
            // then the view can be released to save memory and
            // can be recreated again later when leaving the background.
            if (isInBackgroundMode && Window.Current.Content != null)
            {
                // Some apps may wish to use this helper to explicitly disconnect
                // child references.
                // VisualTreeHelper.DisconnectChildrenRecursive(Window.Current.Content);

                // Clear the view content. Note that views should rely on
                // events like Page.Unloaded to further release resources.
                // Release event handlers in views since references can
                // prevent objects from being collected.
                Window.Current.Content = null;
            }

            // Run the GC to collect released resources.
            GC.Collect();
        }
    }
}
