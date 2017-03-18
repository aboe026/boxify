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
        public string Href { get; set; }
        public string Id { get; set; }
        public string name;
        public string AlbumString { get; set; }
        public Album album;
        public List<Artist> Artists { get; set; }
        public string PreviewUrl { get; set; }
        public int Duration { get; set; }

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Track()
        {
            name = "";
            AlbumString = "";
            album = new Album();
            Artists = new List<Artist>();
            PreviewUrl = "";
            Duration = 0;
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
                if (this.album.Images.Count > 0)
                {
                    return this.album.Images.ElementAt(0);
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
                if (this.Artists.Count > 0)
                {
                    return this.Artists.ElementAt(0).Name;
                }
                return "";
            }
        }

        /// <summary>
        /// Populate the track information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the track JSON object</param>
        /// <returns></returns>
        public async Task SetInfo(string trackString)
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
            if (trackJson.TryGetValue("track", out IJsonValue trackObject))
            {
                JsonObject trackObjectJson = trackObject.GetObject();
                await SetInfoDirect(trackObjectJson.Stringify());
            }
        }

        /// <summary>
        /// Populate track information from JSON information
        /// </summary>
        /// <param name="trackString">The string representation of the track JSON objects elements</param>
        /// <returns></returns>
        public async Task SetInfoDirect(string trackString)
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
            if (trackObjectJson.TryGetValue("href", out IJsonValue trackHref))
            {
                Href = trackHref.GetString();
            }
            if (trackObjectJson.TryGetValue("id", out IJsonValue trackId))
            {
                Id = trackId.GetString();
            }
            if (trackObjectJson.TryGetValue("name", out IJsonValue trackName))
            {
                name = trackName.GetString();
            }
            if (trackObjectJson.TryGetValue("preview_url", out IJsonValue trackPreview))
            {
                if (trackPreview.ToString() != "null")
                {
                    PreviewUrl = trackPreview.GetString();
                }
            }
            if (trackObjectJson.TryGetValue("duration_ms", out IJsonValue trackDuration))
            {
                Duration = Convert.ToInt32(trackDuration.GetNumber());
            }
            if (trackObjectJson.TryGetValue("album", out IJsonValue trackAlbum))
            {
                await album.SetInfo(trackAlbum.Stringify());
            }
            if (trackObjectJson.TryGetValue("artists", out IJsonValue trackArtists))
            {
                JsonArray artistsArray = trackArtists.GetArray();
                foreach (JsonValue artistObject in artistsArray)
                {
                    Artist artist = new Artist();
                    artist.SetInfo(artistObject.Stringify());
                    Artists.Add(artist);
                }
            }
        }

        /// <summary>
        /// Play the track
        /// </summary>
        public void PlayTrack()
        {
            PlaybackService.StartNewSession(Classes.PlaybackSession.PlaybackType.Single, Href);
        }
    }
}
