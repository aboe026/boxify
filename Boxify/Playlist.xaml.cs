using System;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    public sealed partial class Playlist : UserControl
    {
        public enum State { Paused, Playing };

        public string id = "";
        public string href = "";
        public string name = "";
        public string tracksHref = "";
        public int tracksNumber = 0;
        public State state = State.Paused;

        public Playlist()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Sets the information for the Playlist
        /// </summary>
        /// <param name="jsonString">The JSON containing the playlist info</param>
        public void setInfo(string jsonString)
        {
            JsonObject playlistJson = new JsonObject();
            try
            {
                playlistJson = JsonObject.Parse(jsonString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue idJson;
            IJsonValue hrefJson;
            IJsonValue nameJson;
            IJsonValue tracksJson;
            if (playlistJson.TryGetValue("id", out idJson))
            {
                id = idJson.GetString();
            }
            if (playlistJson.TryGetValue("href", out hrefJson))
            {
                href = hrefJson.GetString();
            }
            if (playlistJson.TryGetValue("name", out nameJson))
            {
                name = nameJson.GetString();
            }
            if (playlistJson.TryGetValue("tracks", out tracksJson))
            {
                JsonObject trackJson = tracksJson.GetObject();
                IJsonValue trackHrefJson;
                IJsonValue trackNumberJson;
                if (trackJson.TryGetValue("href", out trackHrefJson))
                {
                    tracksHref = trackHrefJson.GetString();
                }
                if (trackJson.TryGetValue("total", out trackNumberJson))
                {
                    tracksNumber = Convert.ToInt32(trackNumberJson.GetNumber());
                }
            }

            // UI
            displayName.Text = name;
            displayTracksNumber.Text = "Tracks: " + tracksNumber.ToString();
        }

        /// <summary>
        /// The Play/Pause button is clicked
        /// </summary>
        /// <param name="sender">The actionButton that was clicked</param>
        /// <param name="e">The routed event arguments</param>
        private void action_Click(object sender, RoutedEventArgs e)
        {
            Button actionButton = (Button)sender;
            if (state == State.Paused)    // play button
            {
                actionButton.Content = "\uE769";
                actionButton.Foreground = new SolidColorBrush(Colors.Black);
                state = State.Playing;
            }
            else if (state == State.Playing)
            {
                actionButton.Content = "\uE768";
                actionButton.Foreground = new SolidColorBrush(Colors.Green);
                state = State.Paused;
            }
        }
    }
}
