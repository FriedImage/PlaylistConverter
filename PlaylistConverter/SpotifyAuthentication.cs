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
using static PlaylistConverter.AppConfig.TokenStorage;
using System.Collections.ObjectModel;

namespace PlaylistConverter
{
    internal class SpotifyAuthentication
    {
        private static readonly EmbedIOAuthServer _server = new(new Uri("http://localhost:5543/callback"), 5543);
        private static string? _codeVerifier;
        private static SpotifyClient? _spotifyClient;
        private static TaskCompletionSource<SpotifyClient> _spotifyClientTcs = new();
        public static SpotifyClient? SpotifyClientInstance => _spotifyClient;

        public static async Task<SpotifyClient> GetSpotifyClientAsync()
        {
            // If we already have a client instance, return it
            if (_spotifyClient != null) return _spotifyClient;

            // If tokens exist, load and use them to create the client
            if (File.Exists(AppConfig.TokenStorage.spotifyTokenPath))
            {
                var json = await File.ReadAllTextAsync(AppConfig.TokenStorage.spotifyTokenPath);
                var tokenResponse = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

                var authenticator = new PKCEAuthenticator(AppConfig.SpotifyClientId, tokenResponse);
                authenticator.TokenRefreshed += (sender, token) =>
                {
                    SaveTokensAsync(token);  // Save refreshed tokens
                };

                var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
                _spotifyClient = new SpotifyClient(config);

                return _spotifyClient;
            }
            else
            {
                // If no saved tokens, start authentication
                await StartSpotifyAuthentication();
                return await _spotifyClientTcs.Task;
            }
        }

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
                Scope =
                [
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.UserLibraryRead,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadEmail,
                    Scopes.UserLibraryModify
                ]
            };

            BrowserUtil.Open(request.ToUri());

            // Spotify Credentials (for debugging)
            Debug.WriteLine($"Spotify Client Id: {AppConfig.SpotifyClientId}");
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
                new PKCETokenRequest(
                    AppConfig.SpotifyClientId,
                    response.Code,
                    _server.BaseUri,
                    _codeVerifier
                )
            );

            await SaveTokensAsync(tokenResponse);

            // Instantiate SpotifyClient and save it to _spotifyClient
            _spotifyClient = new SpotifyClient(tokenResponse.AccessToken);
            _spotifyClientTcs.TrySetResult(_spotifyClient);
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Debug.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
            _spotifyClientTcs.TrySetException(new Exception(error));
        }

        private static async Task SaveTokensAsync(PKCETokenResponse tokenResponse)
        {
            var json = JsonConvert.SerializeObject(tokenResponse);
            await File.WriteAllTextAsync(AppConfig.TokenStorage.spotifyTokenPath, json);  // Save tokens in a file for future use
        }

        public static async Task<ObservableCollection<PlaylistInfo>> GetPlaylistsAsync()
        {
            var playlists = new ObservableCollection<PlaylistInfo>();

            var spotifyClient = await GetSpotifyClientAsync();
            var userPlaylists = await spotifyClient.Playlists.CurrentUsers();

            foreach (var playlist in userPlaylists.Items)
            {
                playlists.Add(new PlaylistInfo(playlist.Name, playlist.Owner.DisplayName, playlist.Tracks.Total.Value, playlist.Description));
                //{
                //    Name = playlist.Name,
                //    Description = playlist.Description,
                //    Creator = playlist.Owner.DisplayName,
                //    SongCount = playlist.Tracks.Total.Value
                //});
            }

            return playlists;
        }

        // GetSpotifyClient() is newer
        //public static async Task CheckSavedTokensAndAuthenticate()
        //{
        //    if (File.Exists(AppConfig.spotifyTokenPath))
        //    {
        //        var json = await File.ReadAllTextAsync(AppConfig.spotifyTokenPath);
        //        var tokenResponse = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

        //        var authenticator = new PKCEAuthenticator(AppConfig.SpotifyClientId, tokenResponse);
        //        authenticator.TokenRefreshed += (sender, token) =>
        //        {
        //            // Save refreshed tokens
        //            SaveTokensAsync(token);
        //        };

        //        var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        //        var spotify = new SpotifyClient(config);

        //        var me = await spotify.UserProfile.Current();
        //        Debug.WriteLine($"Authenticated using saved tokens as: {me.DisplayName}");
        //    }
        //    else
        //    {
        //        await StartSpotifyAuthentication();  // No saved tokens, start the authentication flow
        //    }
        //}
    }

}
