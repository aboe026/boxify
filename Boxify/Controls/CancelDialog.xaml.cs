using System;
using System.Threading;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    public sealed partial class CancelDialog : UserControl
    {
        public CancellationTokenSource cancelToken;

        public CancelDialog()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Set the track name of the track to cancel download of
        /// </summary>
        /// <param name="trackName">The name of the track to cancel download of</param>
        public void setTrackName(string trackName)
        {
            CancelText.Text = String.Format("Looks like the download of \"{0}\" is taking awhile...", trackName);
        }

        /// <summary>
        /// Set the token to call cancellation of
        /// </summary>
        /// <param name="cancelToken">The token to cancel if the user decides to cancel</param>
        public void setCancelToken(CancellationTokenSource cancelToken)
        {
            this.cancelToken = cancelToken;
        }

        /// <summary>
        /// User decides to cancel download of current song
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (cancelToken != null)
            {
                cancelToken.Cancel();
            }
        }
    }
}
