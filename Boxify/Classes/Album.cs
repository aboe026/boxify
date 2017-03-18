/*******************************************************************
Boxify - A Spotify client for Xbox One
Copyright(C) 2017 Adam Boe

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.If not, see<http://www.gnu.org/licenses/>.
*******************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml.Media.Imaging;

namespace Boxify
{
    /// <summary>
    /// An Album object
    /// </summary>
    public class Album : BindableBase
    {
        private string id;
        public string name;
        public List<Artist> Artists { get; set; }
        public List<BitmapImage> Images { get; set; }
        public string ImageUrl { get; set; }
        private static string tracksHref = "https://api.spotify.com/v1/albums/{0}/tracks";

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Album()
        {
            name = "";
            Artists = new List<Artist>();
            Images = new List<BitmapImage>();
        }

        /// <summary>
        /// The name of the album
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.SetProperty(ref this.name, value); }
        }

        /// <summary>
        /// The main image for the album
        /// </summary>
        public BitmapImage Image
        {
            get
            {
                if (this.Images.Count > 0)
                {
                    return this.Images.ElementAt(0);
                }
                return new BitmapImage();
            }
        }

        /// <summary>
        /// The main artist for the album
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
        /// Populate the album information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the album JSON object</param>
        /// <returns></returns>
        public async Task SetInfo(string albumString)
        {
            JsonObject albumJson = new JsonObject();
            try
            {
                albumJson = JsonObject.Parse(albumString);
            }
            catch (COMException)
            {
                return;
            }
            if (albumJson.TryGetValue("id", out IJsonValue idJson))
            {
                id = idJson.GetString();
            }
            if (albumJson.TryGetValue("name", out IJsonValue nameJson))
            {
                name = nameJson.GetString();
            }
            if (albumJson.TryGetValue("artists", out IJsonValue artistsJson)) {
                JsonArray artistsArray = artistsJson.GetArray();
                foreach (JsonValue artistObject in artistsArray)
                {
                    Artist artist = new Artist();
                    artist.SetInfo(artistObject.Stringify());
                    Artists.Add(artist);
                }
            }
            if (albumJson.TryGetValue("images", out IJsonValue imagesJson)) {
                JsonArray imagesArray = imagesJson.GetArray();
                foreach (JsonValue imageObject in imagesArray)
                {
                    JsonValue urlJson = imageObject.GetObject().GetNamedValue("url");
                    string url = urlJson.GetString();
                    if (ImageUrl == null)
                    {
                        ImageUrl = url;
                    }
                    BitmapImage image = await RequestHandler.DownloadImage(url);
                    Images.Add(image);
                }
            }
        }

        /// <summary>
        /// Get all the tracks in the album
        /// </summary>
        /// <returns>A list of tracks in the album</returns>
        public async Task<List<Track>> GetTracks()
        {
            List<Track> tracks = new List<Track>();
            string tracksString = await RequestHandler.SendCliGetRequest(tracksHref.Replace("{id}", id));
            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return tracks;
            }
            if (tracksJson.TryGetValue("items", out IJsonValue itemsJson))
            {
                JsonArray itemsArray = itemsJson.GetArray();
                foreach (JsonValue trackJson in itemsArray)
                {
                    Track track = new Track();
                    await track.SetInfoDirect(trackJson.Stringify());
                    track.album = this;
                    tracks.Add(track);
                }
            }
            return tracks;
        }
        
        /// <summary>
        /// Play all tracks in Album
        /// </summary>
        /// <returns></returns>
        public void PlayTracks()
        {
            PlaybackService.StartNewSession(Classes.PlaybackSession.PlaybackType.Album, string.Format(tracksHref, id));
        }
    }
}
