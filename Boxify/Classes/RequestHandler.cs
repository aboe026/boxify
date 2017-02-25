using Boxify.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace Boxify
{
    /// <summary>
    /// A class to assist in making REST calls
    /// </summary>
    static class RequestHandler
    {
        public enum SecurityFlow { AuthorizationCode, ClientCredentials };

        public static string callbackUrl = "https://example.com/callback/";
        public static string state = "";
        private static string scopes = "playlist-read-private";
        private static string credentailsFilePath = "ms-appx:///Assets/Credentials.json";
        private static string clientId = "";
        private static string clientSecret = "";
        private static string authorizationBase = "https://accounts.spotify.com/authorize";
        private static string authorizationCode = "";
        private static string tokenBase = "https://accounts.spotify.com/api/token";
        private static string authorizationGrantType = "authorization_code";
        private static string accessToken = "";
        private static DateTime expireTime = new DateTime(DateTime.MinValue.Ticks);
        private static string refreshToken = "";
        private static string clientCredentialsGrantType = "client_credentials";
        private static string ccAccessToken = "";
        private static DateTime ccExpireTime = new DateTime(DateTime.MinValue.Ticks);

        /// <summary>
        /// Builds the authorization uri with required query parameters
        /// </summary>
        /// <returns>The URI to access the authorization tokens</returns>
        public static Uri getAuthorizationUri()
        {
            UriBuilder authorizationBuilder = new UriBuilder(authorizationBase);

            state = generateRandomString(new Random().Next(7, 23));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("client_id", clientId));
            queryParams.Add(new KeyValuePair<string, string>("response_type", "code"));
            queryParams.Add(new KeyValuePair<string, string>("redirect_uri", callbackUrl));
            queryParams.Add(new KeyValuePair<string, string>("scope", scopes));
            queryParams.Add(new KeyValuePair<string, string>("state", state));
            queryParams.Add(new KeyValuePair<string, string>("show_dialog", "true"));
            authorizationBuilder.Query = convertToQueryString(queryParams);

            return authorizationBuilder.Uri;
        }

        /// <summary>
        /// Converts a list of string key-value pairs into a valid URL query parameter delimited by ampersands
        /// </summary>
        /// <param name="queryParams">A list of key-value pairs</param>
        /// <returns>A valid URL query parameter ("vame=value&name2=value2")</returns>
        public static string convertToQueryString(List<KeyValuePair<string, string>> queryParams)
        {
            string queryString = "";
            foreach (KeyValuePair<string, string> param in queryParams)
            {
                queryString += param.Key + "=" + param.Value + "&";
            }
            if (queryString.LastIndexOf("&") > 0)
            {
                queryString = queryString.Substring(0, queryString.Length - 1);
            }
            return queryString;
        }

        /// <summary>
        /// Generates a random string of cryptographic strength by utilizing
        /// the RNGCryptoServiceProvider behind Path.GetRandomFileName
        /// </summary>
        /// <param name="length">The number of random characters to generate</param>
        /// <returns>A string containing the randomly generated characters.</returns>
        private static string generateRandomString(int length)
        {
            string random = "";
            while (random.Length < length)
            {
                random += Path.GetRandomFileName().Replace(".", "");
            }
            return random.Substring(0, length);
        }

        /// <summary>
        /// Reads the Spotify and YouTube credentials from file
        /// </summary>
        public async static Task retrieveCredentials()
        {
            StorageFile credentialsFile;
            try
            {
                credentialsFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(credentailsFilePath)).AsTask().ConfigureAwait(false);
            }
            catch (FileNotFoundException) { return; }
            catch (UnauthorizedAccessException) { return; }
            string credentailsText = await FileIO.ReadTextAsync(credentialsFile);
            JsonObject credentialsJson = new JsonObject();
            try
            {
                credentialsJson = JsonObject.Parse(credentailsText);
            }
            catch (COMException)
            {

            }
            IJsonValue spotifyJson;
            IJsonValue youtubeJson;
            if (credentialsJson.TryGetValue("Spotify", out spotifyJson))
            {
                JsonObject spotifyObject = spotifyJson.GetObject();
                IJsonValue clientIdJson;
                IJsonValue clientSecretJson;
                if (spotifyObject.TryGetValue("clientId", out clientIdJson))
                {
                    clientId = clientIdJson.GetString();
                }
                if (spotifyObject.TryGetValue("clientSecret", out clientSecretJson))
                {
                    clientSecret = clientSecretJson.GetString();
                }
            }
            if (credentialsJson.TryGetValue("YouTube", out youtubeJson))
            {
                JsonObject youtubeObject = youtubeJson.GetObject();
                IJsonValue applicationNameJson;
                IJsonValue apiKeyJson;
                if (youtubeObject.TryGetValue("applicationName", out applicationNameJson))
                {
                    PlaybackSession.youtubeApplicationName = applicationNameJson.GetString();
                }
                if (youtubeObject.TryGetValue("apiKey", out apiKeyJson))
                {
                    PlaybackSession.youtubeApiKey = apiKeyJson.GetString();
                }
            }
        }

        /// <summary>
        /// Reads and sets the token values from file
        /// </summary>
        public async static Task initializeTokens()
        {
            await retrieveCredentials();

            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)roamingSettings.Values["Tokens"];
            if (composite != null)
            {
                accessToken = composite["access_token"] != null ? composite["access_token"].ToString() : "";
                if (composite["expires_in"] != null)
                {
                    expireTime.AddSeconds(Convert.ToDouble(composite["expires_in"].ToString()));
                }
                refreshToken = composite["refresh_token"] != null ? composite["refresh_token"].ToString() : "";
                if (composite["expire_time"] != null)
                {
                    expireTime = new DateTime(Convert.ToInt64(composite["expire_time"].ToString()));
                }
                ccAccessToken = composite["ccAccess_token"] != null ? composite["ccAccess_token"].ToString() : "";
                if (composite["ccExpire_time"] != null)
                {
                    ccExpireTime = new DateTime(Convert.ToInt64(composite["ccExpire_time"].ToString()));
                }
            }

            if (ccAccessToken == "")
            {
                await getClientCredentialsTokens();
            }
            else
            {
                string userJson = await sendAuthGetRequest("https://api.spotify.com/v1/me");
                await UserProfile.updateInfo(userJson);
            }
        }


        /// <summary>
        /// Retrieves the tokens used for subsequent rest authentication based on the Client Credentials security model
        /// </summary>
        /// <returns></returns>
        public async static Task getClientCredentialsTokens()
        {
            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            IBuffer buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(clientId + ":" + clientSecret, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            string base64token = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(buffer);
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Basic", base64token);
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));


            UriBuilder authRequestUri = new UriBuilder(tokenBase);

            // Send the GET request asynchronously and retrieve the response as a string.
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            // body
            string requestContent = string.Format("grant_type={0}",
            Uri.EscapeDataString(clientCredentialsGrantType));

            HttpStringContent body = new HttpStringContent(requestContent, UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");

            try
            {
                httpResponse = await client.PostAsync(authRequestUri.Uri, body);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return;
            }

            await setTokens(httpResponseBody, SecurityFlow.ClientCredentials);
            saveTokens();
        }

        /// <summary>
        /// Retrieves the tokens used for subsequent rest authentication based on the Authorization Code Flow security model
        /// </summary>
        /// <param name="code">The authorization code required to retrieve the tokens</param>
        /// <returns></returns>
        public async static Task getAuthorizationCodeTokens(string code)
        {
            authorizationCode = code;

            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            IBuffer buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(clientId + ":" + clientSecret, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            string base64token = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(buffer);
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Basic", base64token);
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));


            UriBuilder authRequestUri = new UriBuilder(tokenBase);

            // Send the GET request asynchronously and retrieve the response as a string.
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            // body
            string requestContent = string.Format("grant_type={0}&code={1}&redirect_uri={2}",
                Uri.EscapeDataString(authorizationGrantType),
                Uri.EscapeDataString(authorizationCode),
                Uri.EscapeDataString(callbackUrl));

            HttpStringContent body = new HttpStringContent(requestContent, UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");

            try
            {
                httpResponse = await client.PostAsync(authRequestUri.Uri, body);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return;
            }

            await setTokens(httpResponseBody, SecurityFlow.AuthorizationCode);
            saveTokens();
        }

        /// <summary>
        /// Sets the access tokens
        /// </summary>
        /// <param name="tokensString">A JSON string with token data</param>
        /// <returns></returns>
        public async static Task setTokens(string tokensString, SecurityFlow securityFlow)
        {
            JsonObject tokensJson = new JsonObject();
            try
            {
                tokensJson = JsonObject.Parse(tokensString);
            }
            catch (COMException) { }

            IJsonValue accessTokenJson;
            IJsonValue expiresInJson;
            IJsonValue refreshTokenJson;
            IJsonValue expireTimeJson;
            IJsonValue ccAccessTokenJson;
            IJsonValue ccExpireTimeJson;
            if (tokensJson.TryGetValue("access_token", out accessTokenJson))
            {
                if (securityFlow == SecurityFlow.AuthorizationCode)
                {
                    accessToken = accessTokenJson.GetString();
                }
                else if (securityFlow == SecurityFlow.ClientCredentials)
                {
                    ccAccessToken = accessTokenJson.GetString();
                }
            }
            if (tokensJson.TryGetValue("expires_in", out expiresInJson))
            {
                double expiresIn = expiresInJson.GetNumber();
                DateTime currentTime = DateTime.Now;
                if (securityFlow == SecurityFlow.AuthorizationCode)
                {
                    expireTime = currentTime.AddSeconds(expiresIn);
                }
                else if (securityFlow == SecurityFlow.ClientCredentials)
                {
                    ccExpireTime = currentTime.AddSeconds(expiresIn);
                }
            }
            if (tokensJson.TryGetValue("refresh_token", out refreshTokenJson))
            {
                refreshToken = refreshTokenJson.GetString();
            }
            if (tokensJson.TryGetValue("expire_time", out expireTimeJson))
            {
                expireTime = new DateTime(Convert.ToInt64(expireTimeJson.GetString()));
            }
            if (tokensJson.TryGetValue("ccAccess_token", out ccAccessTokenJson))
            {
                ccAccessToken = ccAccessTokenJson.GetString();
            }
            if (tokensJson.TryGetValue("ccExpire_time", out ccExpireTimeJson))
            {
                ccExpireTime = new DateTime(Convert.ToInt64(ccExpireTimeJson.GetString()));
            }

            if (securityFlow == SecurityFlow.AuthorizationCode)
            {
                string userJson = await sendAuthGetRequest("https://api.spotify.com/v1/me");
                await UserProfile.updateInfo(userJson);
            }
        }

        /// <summary>
        /// Writes the tokens to file as JSON
        /// </summary>
        private static void saveTokens()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();

            composite["refresh_token"] = refreshToken;
            composite["access_token"] = accessToken;
            composite["expire_time"] = expireTime.Ticks.ToString();
            composite["ccAccess_token"] = ccAccessToken;
            composite["ccExpire_time"] = ccExpireTime.Ticks.ToString();

            roamingSettings.Values["Tokens"] = composite;
        }

        /// <summary>
        /// Sends a GET request to the Spotify API with the token aquired from the Authorization Code Flow security model
        /// </summary>
        /// <param name="uriString">The Spotify REST endpoint to hit</param>
        /// <returns>The body of the response</returns>
        public async static Task<string> sendAuthGetRequest(string uriString)
        {
            return await sendGetRequest(uriString, accessToken, SecurityFlow.AuthorizationCode);
        }

        /// <summary>
        /// Sends a GET request to the Spotify API with the token aquired from the Client Credentials Flow security model
        /// </summary>
        /// <param name="uriString">The Spotify REST endpoint to hit</param>
        /// <returns>The body of the response</returns>
        public async static Task<string> sendCliGetRequest(string uriString)
        {
            return await sendGetRequest(uriString, ccAccessToken, SecurityFlow.ClientCredentials);
        }


        /// <summary>
        /// Sends a GET request to the Spotify API with required authorization tokens
        /// </summary>
        /// <param name="uriString">The Spotify REST endpoint to hit</param>
        /// <returns>The response body</returns>
        public async static Task<string> sendGetRequest(string uriString, string token, SecurityFlow securityFlow)
        {
            if (securityFlow == SecurityFlow.AuthorizationCode)
            {
                if (DateTime.Now.Ticks > expireTime.Ticks || accessToken == "")
                {
                    await refreshTokens();
                }
            }
            else if (securityFlow == SecurityFlow.ClientCredentials)
            {
                if (DateTime.Now.Ticks > ccExpireTime.Ticks || ccAccessToken == "")
                {
                    await getClientCredentialsTokens();
                }
            }

            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));

            UriBuilder uri = new UriBuilder(uriString);
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            try
            {
                httpResponse = await client.GetAsync(uri.Uri);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            return httpResponseBody;
        }

        /// <summary>
        /// Download an image
        /// </summary>
        /// <param name="url">The URL of the image to download</param>
        /// <returns>The downloaded image</returns>
        public static async Task<BitmapImage> downloadImage(string url)
        {
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
            }
            catch (Exception) { }
            return bitmapImage;
        }

        /// <summary>
        /// Requests new access token with refresh token
        /// </summary>
        /// <returns></returns>
        private async static Task<bool> refreshTokens()
        {
            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            IBuffer buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(clientId + ":" + clientSecret, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            string base64token = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(buffer);
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Basic", base64token);
            client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));


            UriBuilder authRequestUri = new UriBuilder(tokenBase);

            HttpResponseMessage httpResponse = new HttpResponseMessage();
            string httpResponseBody = "";

            // body
            string requestContent = string.Format("grant_type={0}&refresh_token={1}",
                Uri.EscapeDataString("refresh_token"),
                Uri.EscapeDataString(refreshToken));

            HttpStringContent body = new HttpStringContent(requestContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");

            try
            {
                httpResponse = await client.PostAsync(authRequestUri.Uri, body);
                if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    return false;
                }
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return false;
            }

            await setTokens(httpResponseBody, SecurityFlow.AuthorizationCode);
            saveTokens();
            return true;
        }

        /// <summary>
        /// Clears the tokens (unauthorizes the user)
        /// </summary>
        /// <returns></returns>
        public static void clearTokens()
        {
            authorizationCode = "";
            accessToken = "";
            expireTime = new DateTime(DateTime.MinValue.Ticks);
            refreshToken = "";

            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();

            roamingSettings.Values["Tokens"] = null;

            UserProfile.userId = "";
            UserProfile.displalyName = "";
            UserProfile.userPic = new BitmapImage();
        }
    }
}
