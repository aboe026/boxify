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
        public List<Artist> artists { get; set; }
        public List<BitmapImage> images { get; set; }
        public string imageUrl { get; set; }
        private static string tracksHref = "https://api.spotify.com/v1/albums/{0}/tracks";

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
                if (this.images.Count > 0)
                {
                    return this.images.ElementAt(0);
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
                if (this.artists.Count > 0)
                {
                    return this.artists.ElementAt(0).name;
                }
                return "";
            }
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
            IJsonValue idJson;
            IJsonValue nameJson;
            IJsonValue artistsJson;
            IJsonValue imagesJson;
            if (albumJson.TryGetValue("id", out idJson))
            {
                id = idJson.GetString();
            }
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

        /// <summary>
        /// Get all the tracks in the album
        /// </summary>
        /// <returns>A list of tracks in the album</returns>
        public async Task<List<Track>> getTracks()
        {
            List<Track> tracks = new List<Track>();
            string tracksString = await RequestHandler.sendCliGetRequest(tracksHref.Replace("{id}", id));
            JsonObject tracksJson = new JsonObject();
            try
            {
                tracksJson = JsonObject.Parse(tracksString);
            }
            catch (COMException)
            {
                return tracks;
            }
            IJsonValue itemsJson;
            if (tracksJson.TryGetValue("items", out itemsJson))
            {
                JsonArray itemsArray = itemsJson.GetArray();
                foreach (JsonValue trackJson in itemsArray)
                {
                    Track track = new Track();
                    await track.setInfoDirect(trackJson.Stringify());
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
        public void playTracks()
        {
            PlaybackService.startNewSession(Classes.PlaybackSession.PlaybackType.Album, string.Format(tracksHref, id));
        }
    }
}
