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

namespace Boxify
{
    /// <summary>
    /// A Track object
    /// </summary>
    public class Track : IDisposable
    {
        private bool disposed = false;
        public string id = "";
        public string href = "";
        public string name = "";
        public string albumJson = "";
        public Album album = new Album();
        public List<Artist> artists = new List<Artist>();
        public string previewUrl = "";
        public int duration = 0;

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Track()
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
            if (trackObjectJson.TryGetValue("id", out IJsonValue trackId))
            {
                id = trackId.GetString();
            }
            if (trackObjectJson.TryGetValue("href", out IJsonValue trackHref))
            {
                href = trackHref.GetString();
            }
            if (trackObjectJson.TryGetValue("name", out IJsonValue trackName))
            {
                name = trackName.GetString();
            }
            if (trackObjectJson.TryGetValue("preview_url", out IJsonValue trackPreview))
            {
                if (trackPreview.ValueType == JsonValueType.String)
                {
                    previewUrl = trackPreview.GetString();
                }
            }
            if (trackObjectJson.TryGetValue("duration_ms", out IJsonValue trackDuration))
            {
                duration = Convert.ToInt32(trackDuration.GetNumber());
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
                    artists.Add(artist);
                }
            }
        }

        /// <summary>
        /// Play the track
        /// </summary>
        public void PlayTrack()
        {
            App.playbackService.StartNewSession(PlaybackSession.PlaybackType.Single, href);
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
                id = null;
                href = null;
                name = null;
                albumJson = null;
                previewUrl = null;

                album.Dispose();
                album = null;
                while (artists.Count > 0)
                {
                    Artist artist = artists.ElementAt(0);
                    artists.Remove(artist);
                    artist.Dispose();
                    artist = null;
                }
                artists.Clear();
                artists = null;
                previewUrl = null;
            }
        }
    }
}
