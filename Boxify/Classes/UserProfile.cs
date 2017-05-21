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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace Boxify.Classes
{
    public static class UserProfile
    {
        public static string displayName = "";
        public static string userId = "";
        public static BitmapImage userPic = new BitmapImage();

        /// <summary>
        /// Updates the users information (name, image, etc)
        /// </summary>
        /// <param name="jsonString">The JSON containing the users information</param>
        /// <returns></returns>
        public async static Task UpdateInfo(string jsonString)
        {
            // parse data
            JsonObject userJson = new JsonObject();
            try
            {
                userJson = JsonObject.Parse(jsonString);
            }
            catch (COMException)
            {
                return;
            }
            if (userJson.TryGetValue("display_name", out IJsonValue displayNameJson))
            {
                if (displayNameJson.ValueType == JsonValueType.String)
                {
                    displayName = displayNameJson.GetString();
                }
            }
            if (userJson.TryGetValue("id", out IJsonValue userIdJson))
            {
                userId = userIdJson.GetString();
                if (displayName == "")
                {
                    displayName = userIdJson.GetString();
                }
            }

            // picture
            if (userJson.TryGetValue("images", out IJsonValue imagesJson))
            {
                JsonArray imageArray = imagesJson.GetArray();
                if (imageArray.Count > 0)
                {
                    JsonObject imageObject = imageArray.ElementAt(0).GetObject();
                    JsonValue urlJson = imageObject.GetNamedValue("url");
                    string url = urlJson.GetString();
                    UriBuilder uri = new UriBuilder(url);

                    BitmapImage bitmapImage = new BitmapImage();
                    HttpClient client = new HttpClient();
                    HttpResponseMessage httpResponse = new HttpResponseMessage();

                    try
                    {
                        httpResponse = await client.GetAsync(uri.Uri);
                        httpResponse.EnsureSuccessStatusCode();
                        IInputStream st = await client.GetInputStreamAsync(uri.Uri);
                        MemoryStream memoryStream = new MemoryStream();
                        await st.AsStreamForRead().CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                        userPic = bitmapImage;
                        SaveProfileData();
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Save the users information to disk
        /// </summary>
        private static void SaveProfileData()
        {
            //TODO: save user information (name, picture) to disk to limit REST calls
        }

        /// <summary>
        /// Checks whether the user has logged into a Spotify account
        /// </summary>
        /// <returns>True if the user has logged into Spotify, false otherwise</returns>
        public static bool IsLoggedIn()
        {
            if (userId == "")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
