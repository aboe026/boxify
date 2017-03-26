using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class TvMode : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public TvMode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a predetermined toggle value
        /// </summary>
        /// <param name="enabled"></param>
        public TvMode(bool enabled) : this()
        {
            TvModeSwitch.Toggled -= TvModeSwitch_Toggled;
            TvModeSwitch.IsOn = enabled;
            TvModeSwitch.Toggled += TvModeSwitch_Toggled;
        }

        /// <summary>
        /// User wishes to change the TV mode setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TvModeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            MainPage mainPage = (((((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Parent as Grid).Parent as MainPage);
            bool enabled = (sender as ToggleSwitch).IsOn;
            if (mainPage.settingsPage != null)
            {
                mainPage.settingsPage.SetTvSafeUI(enabled);
            }
            else
            {
                Settings.SetTvSafe(enabled);
                if (enabled)
                {
                    mainPage.SafeAreaOn();
                }
                else
                {
                    mainPage.SafeAreaOff();
                }
            }
        }
    }
}
