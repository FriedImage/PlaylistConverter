using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace PlaylistConverter
{
    internal class YoutubeAuthentication
    {
        private string? ClientId;
        private string? ClientSecret;
        private static readonly int _port = 5001;
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";
        private static YouTubeService? _youtubeService;
        public static YouTubeService? YoutubeClientInstance => _youtubeService;

        public YoutubeAuthentication()
        {
            LoadClientSecrets(AppConfig.YoutubeJsonFilePath);
        }

        public static async Task<YouTubeService> GetYouTubeServiceAsync()
        {
            if (_youtubeService != null)
            {
                return _youtubeService;
            }

            _youtubeService = await AuthenticateAsyncLocal();
            return _youtubeService;
        }

        public static async Task<YouTubeService> AuthenticateAsyncLocal()
        {
            UserCredential credential;
            var receiver = new CustomLocalServerCodeReceiver(_port);

            using (var stream = new FileStream(AppConfig.YoutubeJsonFilePath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is
                // created automatically when the authorization flow completes for the first
                // time.
                string credPath = AppConfig.tokenFolderDirectory; // This file will be created in your project directory
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStreamAsync(stream).Result.Secrets,
                    [YouTubeService.Scope.YoutubeForceSsl],
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true),
                    receiver
                    );
            }

            // Create YouTube API service
            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "SpotiTube - Playlist Converter",
            });

            
            //var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            //{
            //    HttpClientInitializer = credential,
            //    ApplicationName = "SpotiTube - Playlist Converter",
            //});

            return _youtubeService;
        }

        private void LoadClientSecrets(string path)
        {
            // Read and parse the client_secrets.json file
            var json = File.ReadAllText(path);
            dynamic? secrets = JsonConvert.DeserializeObject(json);

            ClientId = secrets.web.client_id;
            ClientSecret = secrets.web.client_secret;

            if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            {
                throw new InvalidOperationException("Invalid client_secrets.json file. Missing client_id or client_secret.");
            }
        }










        // Github example approach (Problem met: Port isn't starting up, web-server needed)

        //public async Task AuthenticateAsync()
        //{
        //    string state = RandomDataBase64url(32);
        //    string codeVerifier = RandomDataBase64url(32);
        //    string codeChallenge = Base64urlEncodeNoPadding(Sha256(codeVerifier));
        //    const string CodeChallengeMethod = "S256";

        //    string redirectUri = $"http://{IPAddress.Loopback}:{_port}/";
        //    using (var httpListener = new HttpListener())
        //    {
        //        httpListener.Prefixes.Add(redirectUri);
        //        httpListener.Start();

        //        string authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&client_id={ClientId}" +
        //                                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        //                                      $"&scope=openid%20profile&state={state}" +
        //                                      $"&code_challenge={codeChallenge}&code_challenge_method={CodeChallengeMethod}";

        //        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = authorizationRequest, UseShellExecute = true });

        //        var context = await httpListener.GetContextAsync();
        //        var code = context.Request.QueryString.Get("code");
        //        if (code == null)
        //        {
        //            throw new InvalidOperationException("Authorization failed or was denied.");
        //        }

        //        await ExchangeCodeForTokensAsync(code, codeVerifier, redirectUri);
        //    }
        //}

        //private async Task ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri)
        //{
        //    var tokenRequestBody = new Dictionary<string, string>
        //    {
        //        { "code", code },
        //        { "redirect_uri", redirectUri },
        //        { "client_id", ClientId },
        //        { "client_secret", ClientSecret },
        //        { "code_verifier", codeVerifier },
        //        { "grant_type", "authorization_code" }
        //    };

        //    using (var client = new HttpClient())
        //    {
        //        var requestContent = new FormUrlEncodedContent(tokenRequestBody);
        //        var tokenResponse = await client.PostAsync(TokenEndpoint, requestContent);
        //        var responseBody = await tokenResponse.Content.ReadAsStringAsync();

        //        if (!tokenResponse.IsSuccessStatusCode)
        //        {
        //            throw new Exception("Error during token request: " + responseBody);
        //        }

        //        var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
        //        SaveTokens(tokenData);
        //    }
        //}

        //private void SaveTokens(Dictionary<string, string> tokenData)
        //{
        //    var tokenJson = JsonConvert.SerializeObject(tokenData);
        //    File.WriteAllText(Path.Combine(AppConfig.configJsonDirectory, "youtube_token.json"), tokenJson);
        //}

        //public static int GetRandomUnusedPort()
        //{
        //    var listener = new TcpListener(IPAddress.Loopback, 0);
        //    listener.Start();
        //    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        //    listener.Stop();
        //    return port;
        //}

        //private static string RandomDataBase64url(uint length)
        //{
        //    byte[] bytes = new byte[length];
        //    RandomNumberGenerator.Fill(bytes);
        //    return Base64urlEncodeNoPadding(bytes);
        //}

        //private static byte[] Sha256(string inputString)
        //{
        //    return SHA256.HashData(Encoding.ASCII.GetBytes(inputString));
        //}

        //private static string Base64urlEncodeNoPadding(byte[] buffer)
        //{
        //    string base64 = Convert.ToBase64String(buffer);
        //    return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        //}


    }
}
