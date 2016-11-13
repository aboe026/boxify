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
    static class RequestHandler
    {
        public static string callbackUrl = "https://example.com/callback/";
        private static string clientCredentailsFilePath = "ms-appx:///Assets/ClientCredentials.json";
        private static string clientId = "";
        private static string clientSecret = "";
        private static string authorizationBase = "https://accounts.spotify.com/authorize";
        private static string authorizationCode = "";
        private static string tokenBase = "https://accounts.spotify.com/api/token";
        private static string accessToken = "";
        private static string tokenType = "";
        private static string scope = "";
        private static DateTime expireTime = new DateTime(DateTime.MinValue.Ticks);
        private static string refreshToken = "";

        /// <summary>
        /// Builds the authorization uri with required query parameters
        /// </summary>
        /// <returns>The URI to access the authorization tokens</returns>
        public static Uri getAuthorizationUri()
        {
            UriBuilder authorizationBuilder = new UriBuilder(authorizationBase);

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("client_id", clientId));
            queryParams.Add(new KeyValuePair<string, string>("response_type", "code"));
            queryParams.Add(new KeyValuePair<string, string>("redirect_uri", callbackUrl));
            authorizationBuilder.Query = convertToQueryString(queryParams);

            return authorizationBuilder.Uri;
        }

        /// <summary>
        /// Converts a list of string key-value pairs into a valid URL query parameter delimited by ampersands
        /// </summary>
        /// <param name="queryParams">A list of key-value pairs</param>
        /// <returns>A valid URL query parameter ("vame=value&name2=value2")</returns>
        private static string convertToQueryString(List<KeyValuePair<string, string>> queryParams)
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
        /// Updates client credentials from file
        /// </summary>
        public async static Task refreshClientCredentials()
        {
            StorageFile clientCredentialsFile;
            try
            {
                clientCredentialsFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(clientCredentailsFilePath)).AsTask().ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return;
            }
            string clientCredentailsText = await FileIO.ReadTextAsync(clientCredentialsFile);
            JsonObject clientCredentialsJson = new JsonObject();
            try
            {
                clientCredentialsJson = JsonObject.Parse(clientCredentailsText);
            }
            catch (COMException)
            {

            }
            IJsonValue clientIdJson;
            IJsonValue clientSecretJson;
            if (clientCredentialsJson.TryGetValue("clientId", out clientIdJson))
            {
                clientId = clientIdJson.GetString();
            }
            if (clientCredentialsJson.TryGetValue("clientSecret", out clientSecretJson))
            {
                clientSecret = clientSecretJson.GetString();
            }
        }

        /// <summary>
        /// Retrieves the tokens used for subsequent rest authentication
        /// </summary>
        /// <param name="code">The authorization code required to retrieve the tokens</param>
        /// <returns></returns>
        public async static Task getTokens(string code)
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
                Uri.EscapeDataString("authorization_code"),
                Uri.EscapeDataString(authorizationCode),
                Uri.EscapeDataString(callbackUrl));

            HttpStringContent body = new HttpStringContent(requestContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");

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

            await setTokens(httpResponseBody);
            saveTokens();
        }

        /// <summary>
        /// Sets the access tokens
        /// </summary>
        /// <param name="tokensString">A JSON string with token data</param>
        /// <returns></returns>
        public async static Task setTokens(string tokensString)
        {
            JsonObject tokensJson = new JsonObject();
            try
            {
                tokensJson = JsonObject.Parse(tokensString);
            }
            catch (COMException) { }

            IJsonValue accessTokenJson;
            IJsonValue tokenTypeJson;
            IJsonValue scopeJson;
            IJsonValue expiresInJson;
            IJsonValue refreshTokenJson;
            IJsonValue expireTimeJson;
            if (tokensJson.TryGetValue("access_token", out accessTokenJson))
            {
                accessToken = accessTokenJson.GetString();
            }
            if (tokensJson.TryGetValue("token_type", out tokenTypeJson))
            {
                tokenType = tokenTypeJson.GetString();
            }
            if (tokensJson.TryGetValue("scope", out scopeJson))
            {
                scope = scopeJson.GetString();
            }
            if (tokensJson.TryGetValue("expires_in", out expiresInJson))
            {
                double expiresIn = expiresInJson.GetNumber();
                DateTime currentTime = DateTime.Now;
                expireTime = currentTime.AddSeconds(expiresIn);
            }
            if (tokensJson.TryGetValue("refresh_token", out refreshTokenJson))
            {
                refreshToken = refreshTokenJson.GetString();
            }
            if (tokensJson.TryGetValue("expire_time", out expireTimeJson))
            {
                expireTime = new DateTime(Convert.ToInt64(expireTimeJson.GetString()));
            }

            string userJson = await sendRequest("https://api.spotify.com/v1/me");
            await UserProfile.updateInfo(userJson);
        }

        /// <summary>
        /// Writes the tokens to file as JSON
        /// </summary>
        private async static void saveTokens()
        {
            JsonObject tokensJson = new JsonObject();
            tokensJson.Add("refresh_token", JsonValue.CreateStringValue(refreshToken));
            tokensJson.Add("access_token", JsonValue.CreateStringValue(accessToken));
            tokensJson.Add("expire_time", JsonValue.CreateStringValue(expireTime.Ticks.ToString()));

            StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
            StorageFile dataFile = await roamingFolder.CreateFileAsync("BoxifyTokens.json", CreationCollisionOption.ReplaceExisting);
            try
            {
                await FileIO.WriteTextAsync(dataFile, tokensJson.Stringify());
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Sends a request to the Spotify API with required authorization tokens
        /// </summary>
        /// <param name="uriString">The Spotify REST endpoint to hit</param>
        /// <returns>The response body</returns>
        public async static Task<string> sendRequest(string uriString)
        {
            if (DateTime.Now.Ticks > expireTime.Ticks || accessToken == "")
            {
                await refreshTokens();
            }

            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", accessToken);
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
        /// Requests new access token with refresh token
        /// </summary>
        /// <returns></returns>
        private async static Task refreshTokens()
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
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                return;
            }

            await setTokens(httpResponseBody);
            saveTokens();
        }

        public async static Task clearTokens()
        {
            authorizationCode = "";
            accessToken = "";
            tokenType = "";
            scope = "";
            expireTime = new DateTime(DateTime.MinValue.Ticks);
            refreshToken = "";

            StorageFolder roamingFolder = ApplicationData.Current.RoamingFolder;
            StorageFile dataFile = await roamingFolder.CreateFileAsync("BoxifyTokens.json", CreationCollisionOption.ReplaceExisting);
            try
            {
                await dataFile.DeleteAsync();
            }
            catch (Exception) { }

            UserProfile.displalyName = "";
            UserProfile.userPic = new BitmapImage();
        }
    }
}
