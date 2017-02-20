using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml.Media.Imaging;

namespace Boxify
{
    /// <summary>
    /// A playlist object containing tracks
    /// </summary>
    public class Playlist : BindableBase
    {
        public string id { get; set; }
        public string href { get; set; }
        public string name;
        public string description;
        public string tracksHref { get; set; }
        private const int maxTracksPerRequest = 100;
        public int tracksCount;
        public List<BitmapImage> images { get; set; }
        public BitmapImage image;

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Playlist()
        {
            id = "";
            href = "";
            name = "";
            tracksHref = "";
            tracksCount = 0;
            images = new List<BitmapImage>();
            image = new BitmapImage();
        }

        /// <summary>
        /// The name of the playlist
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.SetProperty(ref this.name, value); }
        }

        /// <summary>
        /// The name of the playlist
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.SetProperty(ref this.description, value); }
        }

        /// <summary>
        /// The number of tracks in the playlist
        /// </summary>
        public int TracksCount
        {
            get { return this.tracksCount; }
            set { this.SetProperty(ref this.tracksCount, value); }
        }

        /// <summary>
        /// The main image for the playlist
        /// </summary>
        public BitmapImage Image
        {
            get { return this.image; }
            set { this.SetProperty(ref this.image, value); }
        }

        /// <summary>
        /// Populate the playlists information from the JSON object
        /// </summary>
        /// <param name="jsonString">The string representation of the playlist JSON object</param>
        /// <returns></returns>
        public async Task setInfo(string jsonString)
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
            IJsonValue descriptionJson;
            IJsonValue tracksJson;
            IJsonValue imagesJson;
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
            if (playlistJson.TryGetValue("description", out descriptionJson))
            {
                description = Regex.Replace(descriptionJson.GetString(), "<.+?>", string.Empty);
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
                    tracksCount = Convert.ToInt32(trackNumberJson.GetNumber());
                }
            }
            if (playlistJson.TryGetValue("images", out imagesJson))
            {
                JsonArray imagesArray = imagesJson.GetArray();
                foreach (JsonValue imageObject in imagesArray)
                {
                    JsonValue urlJson = imageObject.GetObject().GetNamedValue("url");
                    string url = urlJson.GetString();
                    BitmapImage image = await RequestHandler.downloadImage(url);
                    images.Add(image);
                }
                if (images.Count > 0)
                {
                    image = images.ElementAt(0);
                }
            }
        }

        /// <summary>
        /// Play the tracks in the playlist
        /// </summary>
        /// <returns></returns>
        public void playTracks()
        {
            PlaybackService.startNewSession(Classes.PlaybackSession.PlaybackType.Playlist, tracksHref); 
        }
    }
}
