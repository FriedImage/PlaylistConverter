using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using static PlaylistConverter.AppConfig.Tokens;

namespace PlaylistConverter
{
    internal class PlatformAuthentications
    {
        // Spotify Authentications
        public class SpotifyAuthentication
        {
            private static readonly int staticPort = 5000; // Server port for Authentication callbacks

            // Client only needed for NON-User provided Scopes (not used at the moment)
            public static async Task<SpotifyClient> AuthenticateClientAsync()
            {
                var config = SpotifyClientConfig.CreateDefault();
                var request = new ClientCredentialsRequest(AppConfig.SpotifyAPI.GetClientId(), AppConfig.SpotifyAPI.GetSecretKey());
                var response = await new OAuthClient(config).RequestToken(request);

                return new SpotifyClient(config.WithToken(response.AccessToken));
            }

            // User log-in
            public static async Task<SpotifyClient> AuthenticateUserAsync()
            {
                Debug.WriteLine("Starting Spotify authentication");
                var spotifyToken = SpotifyToken.LoadFromFile();
                Debug.WriteLine("This is the token: " + spotifyToken);
                Debug.WriteLine(spotifyToken != null ? "Token loaded successfully" : "No token found");

                if (spotifyToken != null)
                {
                    // Not expired and Token not corrupt
                    if (!spotifyToken.IsExpired && !string.IsNullOrEmpty(spotifyToken.RefreshToken))
                    {
                        // Return existing client if token is valid
                        return new SpotifyClient(spotifyToken.AccessToken);
                    }
                    // Token expired, but refreshToken is available
                    else if (!string.IsNullOrEmpty(spotifyToken.RefreshToken))
                    {
                        try
                        {
                            var refreshedToken = await RefreshTokenAsync(spotifyToken.RefreshToken);
                            refreshedToken.SaveToFile(); // Save the new token after refreshing
                            return new SpotifyClient(refreshedToken.AccessToken);
                        }
                        // If available refresh token is corrupt
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Token refresh failed: {ex.Message}. Initiating full authentication.");
                        }
                    }

                    // Full authentication flow if token is not null but corrupt
                    return await FullAuthenticationFlow();
                }

                // Full authentication flow if no valid token is found
                return await FullAuthenticationFlow();

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
                    Debug.WriteLine("Raw .json response: " + json);

                    // Use a dictionary to inspect raw values
                    var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    // tokenData expected values
                    Debug.WriteLine($"Token data: {string.Join(", ", tokenData.Select(kv => $"{kv.Key}: {kv.Value}"))}");

                    // Create and populate SpotifyToken object
                    var tokenResponse = new SpotifyToken
                    {
                        AccessToken = tokenData["access_token"],
                        RefreshToken = tokenData.TryGetValue("refresh_token", out string? value) ? value : refreshToken, // refresh_token may not be returned
                        ExpiresAt = DateTime.UtcNow.AddSeconds(double.Parse(tokenData["expires_in"]))
                    };

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

                // Stop server after getting authCode (in LocalServer)
                //await LocalServer.StopServer();

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
            private static readonly int staticPort = 5001; // Server port for Authentication callbacks

            // old, specific filepath containing "web" area of secret
            //static readonly string clientSecretPath = Path.Combine(AppConfig.rootPath, "yt_client_secret.json");

            // User log-in
            public static async Task<YouTubeService> AuthenticateAsync()
            {
                var youtubeToken = YoutubeToken.LoadFromFile();

                // Check if the token already exists
                if (youtubeToken != null)
                {
                    // Not expired and Token not corrupt
                    if (!youtubeToken.IsExpired && !string.IsNullOrEmpty(youtubeToken.RefreshToken))
                    {
                        // Token is valid, use it to create YouTubeService
                        return CreateYouTubeService(youtubeToken.AccessToken);
                    }
                    // Token expired, but tokenResponse.RefreshToken is available
                    else if (!string.IsNullOrEmpty(youtubeToken.RefreshToken))
                    {
                        try
                        {
                            var refreshedToken = await RefreshTokenAsync(youtubeToken.RefreshToken);
                            refreshedToken.SaveToFile(); // Save the new token after refreshing
                            return CreateYouTubeService(refreshedToken.AccessToken);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Token refresh failed: {ex.Message}. Initiating full authentication.");
                        }
                    }

                    // Full authentication flow if token is not null but corrupt
                    return await FullAuthenticationFlow();
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
            private static async Task<YoutubeToken> RefreshTokenAsync(string refreshToken)
            {
                var refreshRequestData = new Dictionary<string, string>
                {
                    { "client_id", AppConfig.YoutubeAPI.GetClientId() },
                    { "client_secret", AppConfig.YoutubeAPI.GetClientSecret() },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                using var client = new HttpClient();
                var response = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(refreshRequestData));

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Raw .json response: " + json);

                    //var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                    // Use a dictionary to inspect raw values
                    var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                    // tokenData expected values
                    Debug.WriteLine($"Token data: {string.Join(", ", tokenData.Select(kv => $"{kv.Key}: {kv.Value}"))}");

                    // Create a YoutubeToken from the TokenResponse
                    var newToken = new YoutubeToken
                    {
                        AccessToken = tokenData["access_token"], // You need to extract these values based on the API response
                        RefreshToken = tokenData.TryGetValue("refresh_token", out string? value) ? value : refreshToken, // Keep the same refresh token
                        IssuedUtc = DateTime.UtcNow, // Set to current time
                        ExpiresInSeconds = long.Parse(tokenData["expires_in"]) // Time is long?
                    };

                    return newToken; // Return the new YoutubeToken
                }
                else
                {
                    throw new Exception("Failed to refresh YouTube token: " + response.ReasonPhrase);
                }
            }

            private static async Task<YouTubeService> FullAuthenticationFlow()
            {
                UserCredential credential;

                // Read file directly containing ONLY secret
                //using (var stream = new FileStream(AppConfig.YoutubeAPI.GetClientSecret(), FileMode.Open, FileAccess.Read))
                //{
                //    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                //        GoogleClientSecrets.FromStream(stream).Secrets,
                //        [YouTubeService.Scope.Youtube],
                //        "user",
                //        CancellationToken.None,
                //        //new FileDataStore(youtubeTokenPath, true), // Save tokens to file
                //        null,
                //        new CustomLocalServerCodeReceiver(staticPort)
                //    );
                //}

                // Read from apikeys.json
                using (var stream = new FileStream(AppConfig.jsonFilePath, FileMode.Open, FileAccess.Read))
                // filestream
                using (var reader = new StreamReader(stream))
                {
                    // Get ClientSecret type data from "web" section of jsonObject
                    var clientSecrets = AppConfig.YoutubeAPI.GetGoogleClientSecret().Secrets;

                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        clientSecrets,
                        // Necessary permissions needed
                        [YouTubeService.Scope.Youtube],
                        "user",
                        CancellationToken.None,
                        null,
                        new CustomLocalServerCodeReceiver(staticPort)
                    );
                }

                // Create a YoutubeToken from the credential's token
                var youtubeToken = new YoutubeToken
                {
                    AccessToken = credential.Token.AccessToken, // Get AccessToken from UserCredential
                    RefreshToken = credential.Token.RefreshToken, // Get RefreshToken from UserCredential
                    IssuedUtc = DateTime.UtcNow, // Set to current time
                    ExpiresInSeconds = credential.Token.ExpiresInSeconds // Duration in seconds
                };

                // Save the newly-created token
                youtubeToken.SaveToFile();

                return new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SpotiTube - Playlist Converter"
                });
            }
        }

    }

}
