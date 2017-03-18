using System.Runtime.InteropServices;
using Windows.Data.Json;

namespace Boxify
{
    /// <summary>
    /// An Artist object
    /// </summary>
    public class Artist
    {
        public string Name { get; set; }

        /// <summary>
        /// The main constructor to create an empty instance
        /// </summary>
        public Artist()
        {
            Name = "";
        }

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
            if (trackJson.TryGetValue("name", out IJsonValue nameJson))
            {
                Name = nameJson.GetString();
            }
        }
    }
}
