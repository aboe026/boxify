using System.Runtime.InteropServices;
using Windows.Data.Json;

namespace Boxify
{
    /// <summary>
    /// An Artist object
    /// </summary>
    public class Artist
    {
        public string name { get; set; }

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Artist()
        {
            name = "";
        }

        /// <summary>
        /// Populate the artist information from the JSON object
        /// </summary>
        /// <param name="artistString">The string representation of the artist JSON object</param>
        public void setInfo(string artistString)
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
            IJsonValue nameJson;
            if (trackJson.TryGetValue("name", out nameJson))
            {
                name = nameJson.GetString();
            }
        }
    }
}
