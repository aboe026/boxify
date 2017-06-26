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
    public class Playlist : IDisposable
    {
        private bool disposed = false;
        public string id = "";
        public string href = "";
        public string name = "";
        public string owner = "";
        public string description = "";
        public string tracksHref = "";
        private const int maxTracksPerRequest = 100;
        public int tracksCount = 0;
        public BitmapImage image = new BitmapImage();

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Playlist()
        {

        }

        /// <summary>
        /// Populate the playlists information from the JSON object
        /// </summary>
        /// <param name="jsonString">The string representation of the playlist JSON object</param>
        /// <returns></returns>
        public async Task SetInfo(string jsonString)
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
            if (playlistJson.TryGetValue("id", out IJsonValue idJson))
            {
                id = idJson.GetString();
            }
            if (playlistJson.TryGetValue("href", out IJsonValue hrefJson))
            {
                href = hrefJson.GetString();
            }
            if (playlistJson.TryGetValue("name", out IJsonValue nameJson))
            {
                name = nameJson.GetString();
            }
            if (playlistJson.TryGetValue("owner", out IJsonValue ownerJson) && ownerJson.ValueType == JsonValueType.Object)
            {
                if (ownerJson.GetObject().TryGetValue("id", out IJsonValue ownerIdJson))
                {
                    owner = ownerIdJson.GetString();
                }
            }
            if (playlistJson.TryGetValue("description", out IJsonValue descriptionJson) && descriptionJson.ValueType == JsonValueType.String)
            {
                description = Regex.Replace(descriptionJson.GetString(), "<.+?>", string.Empty);
            }

            if (playlistJson.TryGetValue("tracks", out IJsonValue tracksJson))
            {
                JsonObject trackJson = tracksJson.GetObject();
                if (trackJson.TryGetValue("href", out IJsonValue trackHrefJson))
                {
                    tracksHref = trackHrefJson.GetString();
                }
                if (trackJson.TryGetValue("total", out IJsonValue trackNumberJson))
                {
                    tracksCount = Convert.ToInt32(trackNumberJson.GetNumber());
                }
            }
            if (playlistJson.TryGetValue("images", out IJsonValue imagesJson))
            {
                JsonArray imagesArray = imagesJson.GetArray();
                if (imagesArray.Count > 0)
                {
                    JsonValue imageObject = imagesArray.ElementAt(0) as JsonValue;
                    JsonValue urlJson = imageObject.GetObject().GetNamedValue("url");
                    string url = urlJson.GetString();
                    image = await RequestHandler.DownloadImage(url);
                }
            }
        }

        /// <summary>
        /// Play the tracks in the playlist
        /// </summary>
        /// <returns></returns>
        public void PlayTracks()
        {
            App.playbackService.StartNewSession(PlaybackSession.PlaybackType.Playlist, tracksHref, tracksCount);
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
                image.ClearValue(BitmapImage.UriSourceProperty);
            }
        }
    }
}
