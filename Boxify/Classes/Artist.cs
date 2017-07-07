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

using System;
using System.Runtime.InteropServices;
using Windows.Data.Json;

namespace Boxify
{
    /// <summary>
    /// An Artist object
    /// </summary>
    public class Artist : IDisposable
    {
        private bool disposed = false;
        public string name = "";

        /// <summary>
        /// Populate the artist information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the artist JSON object</param>
        public void SetInfo(string artistString)
        {
            JsonObject trackJson = new JsonObject();
            try
            {
                trackJson = JsonObject.Parse(artistString);
            }
            catch (COMException)
            {
                return;
            }
            if (trackJson.TryGetValue("name", out IJsonValue nameJson) && nameJson.ValueType == JsonValueType.String)
            {
                name = nameJson.GetString();
            }
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
               
            }
        }
    }
}
