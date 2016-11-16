using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Boxify
{
    public sealed partial class Playlist : UserControl
    {
        public string id = "";
        public string href = "";
        public string name = "";
        public string tracksHref = "";
        public int tracksNumber = 0;

        public Playlist()
        {
            this.InitializeComponent();
        }

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
    }
}
