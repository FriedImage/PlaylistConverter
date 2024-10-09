using SpotifyAPI.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.Util.Store;
using System.IO;
using static PlaylistConverter.AppConfig.Tokens;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Requests;
using Newtonsoft.Json;
using System.Net.Http;

namespace PlaylistConverter
{
    internal class PlatformAuthentications
    {
        
        // Spotify Authentications
        public class SpotifyAuthentication
        {
            private static readonly int staticPort = 5000; // Server port for Authentication callbacks
            
            // Client only needed for NON-User provided Scopes
            //public static async Task<SpotifyClient> AuthenticateClientAsync()
            //{
            //    var config = SpotifyClientConfig.CreateDefault();
            //    var request = new ClientCredentialsRequest(AppConfig.SpotifyAPI.GetClientId(), AppConfig.SpotifyAPI.GetSecretKey());
            //    var response = await new OAuthClient(config).RequestToken(request);

            //    return new SpotifyClient(config.WithToken(response.AccessToken));
            //}

            public static async Task<SpotifyClient> AuthenticateUserAsync()
            {
                var spotifyToken = SpotifyToken.LoadFromFile();

                if (spotifyToken != null)
                {
                    if (!spotifyToken.IsExpired)
                    {
                        // Return existing client if token is valid
                        return new SpotifyClient(spotifyToken.AccessToken);
                    }
                    else
                    {
                        var refreshedToken = await RefreshTokenAsync(spotifyToken.RefreshToken);
                        refreshedToken.SaveToFile();
                        return new SpotifyClient(refreshedToken.AccessToken);
                    }
                }
                else
                {
                    // Full authentication flow if no valid token is found
                    return await FullAuthenticationFlow();
                }

                //// if token already exists and not expired
                //if (spotifyToken != null && !spotifyToken.IsExpired)
                //{
                //    // Return existing client if token is valid
                //    return new SpotifyClient(spotifyToken.AccessToken);
                //}
                //// If token doesn't exist or expired
                //else
                //{

                //    var loginRequest = new LoginRequest(
                //        new Uri($"http://localhost:{staticPort}/callback"),
                //        AppConfig.SpotifyAPI.GetClientId(),
                //        LoginRequest.ResponseType.Code
                //    )
                //    {
                //        // Necessary permissions needed
                //        Scope =
                //        [
                //            Scopes.PlaylistReadPrivate,
                //            Scopes.PlaylistReadCollaborative,
                //            Scopes.PlaylistModifyPrivate,
                //            Scopes.PlaylistModifyPublic,
                //            Scopes.UserLibraryRead,
                //            Scopes.UserReadPrivate,
                //            Scopes.UserReadEmail,
                //            Scopes.UserLibraryModify,
                //        ]
                //    };

                //    Uri loginUri = loginRequest.ToUri();

                //    // Open the browser for the user to log in and authorize
                //    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                //    {
                //        FileName = loginUri.ToString(),
                //        UseShellExecute = true
                //    });

                //    // Start local server to listen for the authorization code
                //    string authCode = await LocalServer.StartServer(staticPort);

                //    // Use the auth code to get the access token
                //    var tokenRequest = new SpotifyAPI.Web.AuthorizationCodeTokenRequest(
                //        AppConfig.SpotifyAPI.GetClientId(),
                //        AppConfig.SpotifyAPI.GetSecretKey(),
                //        authCode,
                //        new Uri($"http://localhost:{staticPort}/callback")
                //    );

                //    var tokenResponse = await new OAuthClient().RequestToken(tokenRequest);

                //    // Create and save the token information
                //    var newToken = new SpotifyToken
                //    {
                //        AccessToken = tokenResponse.AccessToken,
                //        RefreshToken = tokenResponse.RefreshToken,
                //        ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                //    };
                //    newToken.SaveToFile();

                //    return new SpotifyClient(newToken.AccessToken);
                //}
            }

            private static async Task<SpotifyToken> RefreshTokenAsync(string refreshToken)
            {
                var refreshRequestData = new Dictionary<string, string>
                {
                    { "client_id", AppConfig.SpotifyAPI.GetClientId() },
                    { "client_secret", AppConfig.SpotifyAPI.GetSecretKey() },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                using var client = new HttpClient();
                var response = await client.PostAsync("https://accounts.spotify.com/api/token", new FormUrlEncodedContent(refreshRequestData));

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<SpotifyToken>(json);
                    return tokenResponse;
                }
                else
                {
                    throw new Exception("Failed to refresh Spotify token: " + response.ReasonPhrase);
                }
            }

            private static async Task<SpotifyClient> FullAuthenticationFlow()
            {
                var loginRequest = new LoginRequest(
                        new Uri($"http://localhost:{staticPort}/callback"),
                        AppConfig.SpotifyAPI.GetClientId(),
                        LoginRequest.ResponseType.Code
                    )
                {
                    // Necessary permissions needed
                    Scope =
                        [
                            Scopes.PlaylistReadPrivate,
                            Scopes.PlaylistReadCollaborative,
                            Scopes.PlaylistModifyPrivate,
                            Scopes.PlaylistModifyPublic,
                            Scopes.UserLibraryRead,
                            Scopes.UserReadPrivate,
                            Scopes.UserReadEmail,
                            Scopes.UserLibraryModify,
                        ]
                };

                Uri loginUri = loginRequest.ToUri();

                // Open the browser for the user to log in and authorize
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = loginUri.ToString(),
                    UseShellExecute = true
                });

                // Start local server to listen for the authorization code
                string authCode = await LocalServer.StartServer(staticPort);

                // Use the auth code to get the access token
                var tokenRequest = new SpotifyAPI.Web.AuthorizationCodeTokenRequest(
                    AppConfig.SpotifyAPI.GetClientId(),
                    AppConfig.SpotifyAPI.GetSecretKey(),
                    authCode,
                    new Uri($"http://localhost:{staticPort}/callback")
                );

                var tokenResponse = await new OAuthClient().RequestToken(tokenRequest);

                // Create and save the token information
                var newToken = new SpotifyToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                };
                newToken.SaveToFile();

                return new SpotifyClient(newToken.AccessToken);
            }
        }

        // YouTube Authentications
        public class YoutubeAuthentication
        {
            private readonly static int staticPort = 5001; // Server port for Authentication callbacks
            static readonly string clientSecretPath = Path.Combine(AppConfig.rootPath, "yt_client_secret.json");

            public static async Task<YouTubeService> AuthenticateAsync()
            {
                var tokenResponse = YoutubeToken.LoadFromFile();

                // Check if the token already exists
                if (tokenResponse != null)
                {
                    // Check if token has expired
                    if (!tokenResponse.IsExpired)
                    {
                        // Token is valid, use it to create YouTubeService
                        return CreateYouTubeService(tokenResponse.AccessToken);
                    }
                    else
                    {
                        // Token expired, refresh it
                        var refreshedToken = await RefreshTokenAsync(tokenResponse.RefreshToken);
                        refreshedToken.SaveToFile();
                        return CreateYouTubeService(refreshedToken.AccessToken);
                    }
                }

                // Full authentication flow if no valid token is found
                return await FullAuthenticationFlow();
            }

            private static YouTubeService CreateYouTubeService(string accessToken)
            {
                return new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                    ApplicationName = "SpotiTube - Playlist Converter"
                });
            }

            // Refreshes expired youtubeToken
            private static async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
            {
                var refreshRequestData = new Dictionary<string, string>
                {
                    { "client_id", AppConfig.YoutubeAPI.GetClientId() },
                    { "client_secret", AppConfig.YoutubeAPI.GetSecretKey() },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                using var client = new HttpClient();
                // client
                var response = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(refreshRequestData));
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TokenResponse>(json);
            }

            private static async Task<YouTubeService> FullAuthenticationFlow()
            {
                UserCredential credential;

                using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        [YouTubeService.Scope.Youtube],
                        "user",
                        CancellationToken.None,
                        new FileDataStore(youtubeTokenPath), // Save tokens to file
                        new CustomLocalServerCodeReceiver(staticPort)
                    );
                }

                // Save the newly-created token
                YoutubeToken.SaveToFile(credential.Token);

                return new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SpotiTube - Playlist Converter"
                });
            }
        }



    }

}
