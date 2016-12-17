using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml.Media.Imaging;

namespace Boxify
{
    /// <summary>
    /// An Album object
    /// </summary>
    public class Album
    {
        public string name { get; set; }
        public List<Artist> artists { get; set; }
        public List<BitmapImage> images { get; set; }
        public string imageUrl { get; set; }

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Album()
        {
            name = "";
            artists = new List<Artist>();
            images = new List<BitmapImage>();
        }

        /// <summary>
        /// Populate the album information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the album JSON object</param>
        /// <returns></returns>
        public async Task setInfo(string albumString)
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
            IJsonValue nameJson;
            IJsonValue artistsJson;
            IJsonValue imagesJson;
            if (albumJson.TryGetValue("name", out nameJson))
            {
                name = nameJson.GetString();
            }
            if (albumJson.TryGetValue("artists", out artistsJson)) {
                JsonArray artistsArray = artistsJson.GetArray();
                foreach (JsonValue artistObject in artistsArray)
                {
                    Artist artist = new Artist();
                    artist.setInfo(artistObject.Stringify());
                    artists.Add(artist);
                }
            }
            if (albumJson.TryGetValue("images", out imagesJson)) {
                JsonArray imagesArray = imagesJson.GetArray();
                foreach (JsonValue imageObject in imagesArray)
                {
                    JsonValue urlJson = imageObject.GetObject().GetNamedValue("url");
                    string url = urlJson.GetString();
                    if (imageUrl == null)
                    {
                        imageUrl = url;
                    }
                    BitmapImage image = await RequestHandler.downloadImage(url);
                    images.Add(image);
                }
            }
        }
    }
}
