using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Boxify.Settings;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify.Controls.Announcements
{
    public sealed partial class ThemeMode : UserControl
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public ThemeMode()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Construct the object with a pre-defined theme
        /// </summary>
        /// <param name="theme">The current theme of the app</param>
        public ThemeMode(Theme theme) : this()
        {
            System.Click -= ThemeMode_Click;
            Light.Click -= ThemeMode_Click;
            Dark.Click -= ThemeMode_Click;
            if (theme == Theme.System)
            {
                System.IsChecked = true;
            }
            else if (theme == Theme.Light)
            {
                Light.IsChecked = true;
            }
            else if (theme == Theme.Dark)
            {
                Dark.IsChecked = true;
            }
            System.Click += ThemeMode_Click;
            Light.Click += ThemeMode_Click;
            Dark.Click += ThemeMode_Click;
        }

        /// <summary>
        /// User selects to change the theme mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeMode_Click(object sender, RoutedEventArgs e)
        {
            Theme newTheme = Theme.System;
            if ((sender as RadioButton).Name == "System")
            {
                newTheme = Theme.System;
            }
            else if ((sender as RadioButton).Name == "Light")
            {
                newTheme = Theme.Light;
            }
            else if ((sender as RadioButton).Name == "Dark")
            {
                newTheme = Theme.Dark;
            }

            MainPage mainPage = (((((this.Parent as ContentControl).Parent as Border).Parent as RelativePanel).Parent as Grid).Parent as MainPage);
            if (mainPage.settingsPage != null)
            {
                mainPage.settingsPage.SetThemeUI(newTheme);
            }
            else
            {
                Settings.SetTheme(newTheme);
                if (newTheme == Theme.System)
                {
                    if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                    {
                        mainPage.RequestedTheme = ElementTheme.Light;
                    }
                    else if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    {
                        mainPage.RequestedTheme = ElementTheme.Dark;
                    }
                }
                else if (newTheme == Theme.Light)
                {
                    mainPage.RequestedTheme = ElementTheme.Light;
                }
                else if (newTheme == Theme.Dark)
                {
                    mainPage.RequestedTheme = ElementTheme.Dark;
                }
            }
        }
    }
}
