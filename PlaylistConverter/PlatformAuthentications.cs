using SpotifyAPI.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.Util.Store;
using System.IO;

namespace PlaylistConverter
{
    internal class PlatformAuthentications
    {
        
        // Spotify Authentications
        public class SpotifyAuthentication
        {
            private readonly static int staticPort = 5000; // Server port for Authentication callbacks

            public static async Task<SpotifyClient> AuthenticateClientAsync()
            {
                var config = SpotifyClientConfig.CreateDefault();
                var request = new ClientCredentialsRequest(AppConfig.SpotifyAPI.GetClientId(), AppConfig.SpotifyAPI.GetSecretKey());
                var response = await new OAuthClient(config).RequestToken(request);

                return new SpotifyClient(config.WithToken(response.AccessToken));
            }

            public static async Task<SpotifyClient> AuthenticateUserAsync()
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
                var tokenRequest = new AuthorizationCodeTokenRequest(
                    AppConfig.SpotifyAPI.GetClientId(),
                    AppConfig.SpotifyAPI.GetSecretKey(),
                    authCode,
                    new Uri($"http://localhost:{staticPort}/callback")
                );

                var tokenResponse = await new OAuthClient().RequestToken(tokenRequest);

                return new SpotifyClient(tokenResponse.AccessToken);
            }

        }

        // YouTube Authentications
        public class YoutubeAuthentication
        {
            private readonly static int staticPort = 5001; // Server port for Authentication callbacks

            readonly static string clientSecretPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\yt_client_secret.json");

            public static async Task<YouTubeService> AuthenticateAsync()
            {
                UserCredential credential;

                using (var stream = new FileStream(clientSecretPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        [
                            YouTubeService.Scope.Youtube
                        ],
                        "user",
                        CancellationToken.None,
                        new FileDataStore("YouTube.Auth.Store"),
                        new CustomLocalServerCodeReceiver(staticPort)
                    );
                }

                return new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "SpotiTube - Playlist Converter"
                });
            }
        }



    }

}
