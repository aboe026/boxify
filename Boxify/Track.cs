using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Boxify
{
    /// <summary>
    /// A Track object
    /// </summary>
    public class Track
    {
        public string id { get; set; }
        public string name { get; set; }
        public string albumString { get; set; }
        public Album album { get; set; }
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
                    previewUrl = trackPreview.GetString();
                }
                if (trackObjectJson.TryGetValue("duration_ms", out trackDuration))
                {
                    duration = Convert.ToInt32(trackDuration.GetNumber());
                }
                if (trackObjectJson.TryGetValue("album", out trackAlbum)) {
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
        }
    }
}
