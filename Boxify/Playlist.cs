using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public enum State { Paused, Playing };

        public string id { get; set; }
        public string href { get; set; }
        public string name;
        public string tracksHref { get; set; }
        public int tracksCount;
        public List<BitmapImage> images { get; set; }
        public BitmapImage image;
        public State state { get; set; }

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
            state = State.Paused;
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
                image = images.ElementAt(0);
            }
        }

        /// <summary>
        /// Get the list of the playlists tracks
        /// </summary>
        /// <returns>A list of the playlists tracks</returns>
        public async Task<List<Track>> getTracks()
        {
            List<Track> tracks = new List<Track>();
            string tracksString = await RequestHandler.sendCliGetRequest(tracksHref);
            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return tracks;
            }
            IJsonValue itemsJson;
            if (tracksJson.TryGetValue("items", out itemsJson)){
                JsonArray tracksArray = itemsJson.GetArray();
                foreach (JsonValue trackJson in tracksArray)
                {
                    Track track = new Track();
                    tracks.Add(track);
                    await track.setInfo(trackJson.Stringify());
                }
            }
            return tracks;
        }
    }
}
