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

using Boxify.Classes;
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
    /// An Album object
    /// </summary>
    public class Album : IDisposable
    {
        private bool disposed = false;
        public string id = "";
        public string name = "";
        public string releaseDate = "";
        public int tracksCount = 1;
        public List<Artist> artists = new List<Artist>();
        public BitmapImage image = new BitmapImage();
        public string imageUrl = "";
        public string href = "";
        private const string TRACKS_HREF = "https://api.spotify.com/v1/albums/{0}/tracks";

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Album()
        {

        }

        /// <summary>
        /// Get the name of the first Artist
        /// </summary>
        /// <returns>The name of the first Artist</returns>
        public string GetMainArtistName()
        {
            if (this.artists.Count > 0)
            {
                return this.artists.ElementAt(0).name;
            }
            return "";
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
            if (albumJson.TryGetValue("id", out IJsonValue idJson) && idJson.ValueType == JsonValueType.String)
            {
                id = idJson.GetString();
            }
            if (albumJson.TryGetValue("name", out IJsonValue nameJson) && nameJson.ValueType == JsonValueType.String)
            {
                name = nameJson.GetString();
            }
            if (albumJson.TryGetValue("href", out IJsonValue hrefJson) && hrefJson.ValueType == JsonValueType.String)
            {
                href = hrefJson.GetString();

                // extra request to get release date
                UriBuilder fullUriBuilder = new UriBuilder(href);
                string fullString = await RequestHandler.SendCliGetRequest(fullUriBuilder.Uri.ToString());
                JsonObject fullJson = new JsonObject();
                try
                {
                    fullJson = JsonObject.Parse(fullString);
                }
                catch (COMException)
                {
                    return;
                }

                if (fullJson.TryGetValue("release_date", out IJsonValue releaseJson) && releaseJson.ValueType == JsonValueType.String)
                {
                    releaseDate = releaseJson.GetString();
                }
            }
            if (albumJson.TryGetValue("artists", out IJsonValue artistsJson) && artistsJson.ValueType == JsonValueType.Array) {
                JsonArray artistsArray = artistsJson.GetArray();
                foreach (JsonValue artistObject in artistsArray)
                {
                    Artist artist = new Artist();
                    artist.SetInfo(artistObject.Stringify());
                    artists.Add(artist);
                }
            }
            if (albumJson.TryGetValue("images", out IJsonValue imagesJson) && imagesJson.ValueType == JsonValueType.Array) {
                JsonArray imagesArray = imagesJson.GetArray();
                if (imagesArray.Count > 0)
                {
                    JsonValue imageObject = imagesArray.ElementAt(0) as JsonValue;
                    JsonValue urlJson = imageObject.GetObject().GetNamedValue("url");
                    string url = urlJson.GetString();
                    imageUrl = url;
                    image = await RequestHandler.DownloadImage(url);
                }
            }

            // need extra request to get total tracks
            UriBuilder tracksUriBuilder = new UriBuilder(string.Format(TRACKS_HREF, id));
            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("limit", "1")
                };
            string queryParamsString = RequestHandler.ConvertToQueryString(queryParams);
            tracksUriBuilder.Query = queryParamsString;
            string tracksString = await RequestHandler.SendCliGetRequest(tracksUriBuilder.Uri.ToString());
            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return;
            }

            if (tracksJson.TryGetValue("total", out IJsonValue totalJson) && totalJson.ValueType == JsonValueType.Number)
            {
                tracksCount = Convert.ToInt32(totalJson.GetNumber());
            }
        }

        /// <summary>
        /// Get all the tracks in the album
        /// </summary>
        /// <returns>A list of tracks in the album</returns>
        public async Task<List<Track>> GetTracks()
        {
            List<Track> tracks = new List<Track>();
            string tracksString = await RequestHandler.SendCliGetRequest(string.Format(TRACKS_HREF, id));
            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return tracks;
            }
            if (tracksJson.TryGetValue("items", out IJsonValue itemsJson) && itemsJson.ValueType == JsonValueType.Array)
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
            App.playbackService.StartNewSession(PlaybackSession.PlaybackType.Album, string.Format(TRACKS_HREF, id), tracksCount);
        }

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (disposing)
            {
                while (artists.Count > 0)
                {
                    Artist artist = artists.ElementAt(0);
                    artists.Remove(artist);
                    artist.Dispose();
                }
                artists.Clear();

                image.ClearValue(BitmapImage.UriSourceProperty);
            }
        }
    }
}
