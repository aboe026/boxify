using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace Boxify
{
    public static class UserProfile
    {
        public static string displalyName { get; set; } = "";
        public static string userId = "";
        public static BitmapImage userPic = new BitmapImage();

        /// <summary>
        /// Updates the users information (name, image, etc)
        /// </summary>
        /// <param name="jsonString">The JSON containing the users information</param>
        /// <returns></returns>
        public async static Task updateInfo(string jsonString)
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
            IJsonValue displayNameJson;
            IJsonValue userIdJson;
            if (userJson.TryGetValue("display_name", out displayNameJson))
            {
                displalyName = displayNameJson.GetString();
            }
            if (userJson.TryGetValue("id", out userIdJson))
            {
                userId = userIdJson.GetString();
            }

            // picture
            IJsonValue imagesJson;
            if (userJson.TryGetValue("images", out imagesJson))
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
                        var memoryStream = new MemoryStream();
                        await st.AsStreamForRead().CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        await bitmapImage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                        userPic = bitmapImage;
                        saveProfileData();
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Save the users information to disk
        /// </summary>
        private static void saveProfileData()
        {
            //TODO: save user information (name, picture) to disk to limit REST calls
        }

        /// <summary>
        /// Checks whether the user has logged into a Spotify account
        /// </summary>
        /// <returns>True if the user has logged into Spotify, false otherwise</returns>
        public static bool isLoggedIn()
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
