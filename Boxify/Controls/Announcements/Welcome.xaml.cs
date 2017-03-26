using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class Welcome : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public Welcome()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// User wishes to close the Announcements flipview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            (((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// User wishes to continue on and configure settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {

            (((((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Parent as Grid).Parent as MainPage).RightAnnouncement_Click(null, null);
        }
    }
}
