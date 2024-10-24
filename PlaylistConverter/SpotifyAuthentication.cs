using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using static PlaylistConverter.AppConfig.Tokens;

namespace PlaylistConverter
{
    internal static class SpotifyAuthentication
    {
        private static readonly EmbedIOAuthServer _server = new(new Uri("http://localhost:5543/callback"), 5543);
        private static string _codeVerifier;

        public static async Task StartSpotifyAuthentication()
        {
            // Generate PKCE codes
            var (codeVerifier, codeChallenge) = PKCEUtil.GenerateCodes();
            _codeVerifier = codeVerifier;  // Store the verifier for later

            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, AppConfig.SpotifyClientId, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = codeChallenge, // Include the code challenge in the request
                CodeChallengeMethod = "S256",  // This specifies that we are using PKCE
                Scope = new List<string>
                {
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.UserLibraryRead,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadEmail,
                    Scopes.UserLibraryModify
                }
            };

            BrowserUtil.Open(request.ToUri());

            // Spotify Credentials (for debugging)
            Debug.WriteLine($"Spotify Client Id: {AppConfig.SpotifyClientId}");
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            // Exchange authorization code for tokens using the PKCE verifier (no ClientSecret needed)
            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
                new PKCETokenRequest(
                    AppConfig.SpotifyClientId,
                    response.Code,
                    _server.BaseUri,
                    _codeVerifier // Use the saved code verifier to exchange the code for tokens
                )
            );

            // Save tokens persistently
            await SaveTokensAsync(tokenResponse);

            // You can now create a SpotifyClient with the tokens
            var spotify = new SpotifyClient(tokenResponse.AccessToken);

            var me = await spotify.UserProfile.Current();
            Debug.WriteLine($"Logged as: {me.DisplayName}");
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Debug.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }

        private static async Task SaveTokensAsync(PKCETokenResponse tokenResponse)
        {
            var json = JsonConvert.SerializeObject(tokenResponse);
            await File.WriteAllTextAsync(AppConfig.spotifyTokenPath, json);  // Save tokens in a file for future use
        }

        public static async Task CheckSavedTokensAndAuthenticate()
        {
            if (File.Exists(AppConfig.spotifyTokenPath))
            {
                var json = await File.ReadAllTextAsync(AppConfig.spotifyTokenPath);
                var tokenResponse = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

                var authenticator = new PKCEAuthenticator(AppConfig.SpotifyClientId, tokenResponse);
                authenticator.TokenRefreshed += (sender, token) =>
                {
                    // Save refreshed tokens
                    SaveTokensAsync(token);
                };

                var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
                var spotify = new SpotifyClient(config);

                var me = await spotify.UserProfile.Current();
                Debug.WriteLine($"Authenticated using saved tokens as: {me.DisplayName}");
            }
            else
            {
                await StartSpotifyAuthentication();  // No saved tokens, start the authentication flow
            }
        }

        public static PKCETokenResponse? LoadFile()
        {
            if (File.Exists(spotifyTokenPath))
            {
                var json = File.ReadAllText(spotifyTokenPath);
                return JsonConvert.DeserializeObject<PKCETokenResponse>(json);
            }

            return null;
        }
    }

}
