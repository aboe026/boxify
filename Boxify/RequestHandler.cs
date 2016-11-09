using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using Windows.Storage;
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
        private static int expiresIn = int.MaxValue;
        private static string refreshToken = "";

        public static Uri getAuthorizationUri()
        {
            UriBuilder authorizationBuilder = new UriBuilder(authorizationBase);
            refreshClientCredentials();

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("client_id", clientId));
            queryParams.Add(new KeyValuePair<string, string>("response_type", "code"));
            queryParams.Add(new KeyValuePair<string, string>("redirect_uri", callbackUrl));
            authorizationBuilder.Query = convertToQueryString(queryParams);

            return authorizationBuilder.Uri;
        }

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

        public async static void refreshClientCredentials()
        {
            StorageFile clientCredentialsFile;
            try
            {
                clientCredentialsFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(clientCredentailsFilePath));
            }
            catch (FileNotFoundException ex)
            {
                return;
            }
            string clientCredentailsText = await FileIO.ReadTextAsync(clientCredentialsFile);
            JsonObject clientCredentialsJson = new JsonObject();
            try
            {
                clientCredentialsJson = JsonObject.Parse(clientCredentailsText);
            }
            catch (COMException ex)
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

        public async static void setTokens(string code)
        {
            authorizationCode = code;

            // Create an HTTP client object
            HttpClient client = new HttpClient();

            //Add authorization header to the GET request.
            var buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(clientId + ":" + clientSecret, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
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

                // Parse out data
                JsonObject tokensJson = new JsonObject();
                try
                {
                    tokensJson = JsonObject.Parse(httpResponseBody);
                }
                catch (COMException ex)
                {

                }
                IJsonValue accessTokenJson;
                IJsonValue tokenTypeJson;
                IJsonValue scopeJson;
                IJsonValue expiresInJson;
                IJsonValue refreshTokenJson;
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
                    expiresIn = (int)expiresInJson.GetNumber();
                }
                if (tokensJson.TryGetValue("refresh_token", out refreshTokenJson))
                {
                    refreshToken = refreshTokenJson.GetString();
                }
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }
    }
}
