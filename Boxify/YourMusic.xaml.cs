using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Boxify
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class YourMusic : Page
    {
        private static MainPage mainPage;
        private static string playlistsHref = "https://api.spotify.com/v1/me/playlists";
        private static List<Playlist> playlists;

        public YourMusic()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// When the user navigates to this page
        /// </summary>
        /// <param name="e">The navigation event arguments</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                mainPage = (MainPage)e.Parameter;
            }
            if (playlists == null)
            {
                await setPlaylists();
            }
        }

        private async Task setPlaylists()
        {
            string playlistsString = await RequestHandler.sendRequest(playlistsHref);
            JsonObject playlistsJson = new JsonObject();
            try
            {
                playlistsJson = JsonObject.Parse(playlistsString);
            }
            catch (COMException)
            {
                return;
            }
            IJsonValue itemsJson;
            if (playlistsJson.TryGetValue("items", out itemsJson))
            {
                JsonArray playlistsArray = itemsJson.GetArray();
                foreach (JsonValue playlistJson in playlistsArray)
                {

                    Playlist playlist = new Playlist();
                    playlist.setInfo(playlistJson.Stringify());
                    playlistList.Items.Add(playlist);
                    Playlist test = (Playlist)playlistList.Items.ElementAt(0);
                    double test2 = test.Width;
                }
            }
        }
    }
}
