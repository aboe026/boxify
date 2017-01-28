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
    /// A Track object
    /// </summary>
    public class Track : BindableBase
    {
        public string id { get; set; }
        public string name;
        public string albumString { get; set; }
        public Album album;
        public List<Artist> artists { get; set; }
        public string previewUrl { get; set; }
        public int duration { get; set; }

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Track()
        {
            name = "";
            albumString = "";
            album = new Album();
            artists = new List<Artist>();
            previewUrl = "";
            duration = 0;
        }

        /// <summary>
        /// The name of the track
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.SetProperty(ref this.name, value); }
        }

        /// <summary>
        /// The main image for the track
        /// </summary>
        public BitmapImage Image
        {
            get
            {
                if (this.album.images.Count > 0)
                {
                    return this.album.images.ElementAt(0);
                }
                return new BitmapImage();
            }
        }

        /// <summary>
        /// The main artist for the track
        /// </summary>
        public string ArtistName
        {
            get
            {
                if (this.artists.Count > 0)
                {
                    return this.artists.ElementAt(0).name;
                }
                return "";
            }
        }

        /// <summary>
        /// Populate the track information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the track JSON object</param>
        /// <returns></returns>
        public async Task setInfo(string trackString)
        {
            JsonObject trackJson = new JsonObject();
            try
            {
                trackJson = JsonObject.Parse(trackString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue trackObject;
            if (trackJson.TryGetValue("track", out trackObject))
            {
                JsonObject trackObjectJson = trackObject.GetObject();
                await setInfoDirect(trackObjectJson.Stringify());
            }
        }

        /// <summary>
        /// Populate track information from JSON information
        /// </summary>
        /// <param name="trackString">The string representation of the track JSON objects elements</param>
        /// <returns></returns>
        public async Task setInfoDirect(string trackString)
        {
            JsonObject trackObjectJson = new JsonObject();
            try
            {
                trackObjectJson = JsonObject.Parse(trackString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue trackId;
            IJsonValue trackName;
            IJsonValue trackPreview;
            IJsonValue trackDuration;
            IJsonValue trackAlbum;
            IJsonValue trackArtists;
            if (trackObjectJson.TryGetValue("id", out trackId))
            {
                id = trackId.GetString();
            }
            if (trackObjectJson.TryGetValue("name", out trackName))
            {
                name = trackName.GetString();
            }
            if (trackObjectJson.TryGetValue("preview_url", out trackPreview))
            {
                if (trackPreview.ToString() != "null")
                {
                    previewUrl = trackPreview.GetString();
                }
            }
            if (trackObjectJson.TryGetValue("duration_ms", out trackDuration))
            {
                duration = Convert.ToInt32(trackDuration.GetNumber());
            }
            if (trackObjectJson.TryGetValue("album", out trackAlbum))
            {
                await album.setInfo(trackAlbum.Stringify());
            }
            if (trackObjectJson.TryGetValue("artists", out trackArtists))
            {
                JsonArray artistsArray = trackArtists.GetArray();
                foreach (JsonValue artistObject in artistsArray)
                {
                    Artist artist = new Artist();
                    artist.setInfo(artistObject.Stringify());
                    artists.Add(artist);
                }
            }
        }

        /// <summary>
        /// Play the track
        /// </summary>
        public async void playTrack()
        {
            await PlaybackService.playTrack(this, 1, Settings.playbackSource);
        }
    }
}
