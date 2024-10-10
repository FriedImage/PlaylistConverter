using System.Diagnostics;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlaylistConverter
{
    internal class AppConfig
    {
        //static readonly string jsonFileContent = File.ReadAllText("apikeys.json"); 
        //private static dynamic apiKeys;

        public static readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string rootPath = Path.Combine(baseDirectory, @"..\..\..\");
        public static readonly string jsonFilePath = Path.Combine(rootPath, "apikeys.json");
        private static readonly JObject jsonContent;

        static AppConfig()
        {
            string jsonFileContent = File.ReadAllText(jsonFilePath); // Reads content from json file
            jsonContent = JObject.Parse(jsonFileContent);
        }

        public static class Tokens
        {
            // folder containing the tokens
            public static string tokenFolderDirectory = Path.Combine(rootPath, "tokens");
            // type: JSON
            public static readonly string spotifyTokenPath = Path.Combine(tokenFolderDirectory, "spotify_token.json");
            // type: TOKENRESPONSE-USER
            public static readonly string youtubeTokenPath = Path.Combine(tokenFolderDirectory, "youtube_token.TokenResponse-user");

            public static string GetTokenStorageFolderDirectory()
            {
                if (!Directory.Exists(tokenFolderDirectory))
                {
                    Directory.CreateDirectory(tokenFolderDirectory);
                }

                return tokenFolderDirectory;
            }

            // Moves current tokenFolderDirectory into a new directory (newTokenFolderDirectory)
            public static void SetAndReplaceTokenStorageFolderDirectory(string existingTokenFolderDirectory, string newTokenFolderDirectory)
            {
                if (Directory.Exists(Path.Combine(rootPath, existingTokenFolderDirectory)))
                {
                    // Replace tokenFolderDirectory with New Directory
                    tokenFolderDirectory = Path.Combine(rootPath, newTokenFolderDirectory);
                    Directory.Move(existingTokenFolderDirectory, tokenFolderDirectory);
                }
            }

            // Token config for Spotify
            public class SpotifyToken
            {
                // Spotify's token properties
                public required string AccessToken { get; set; }
                public required string RefreshToken { get; set; }
                public DateTime ExpiresAt { get; set; }
                public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

                public static SpotifyToken? LoadFromFile()
                {
                    if (TokenExists())
                    {
                        var json = File.ReadAllText(spotifyTokenPath);
                        return JsonConvert.DeserializeObject<SpotifyToken>(json);
                    }

                    return null;
                }

                public static bool TokenExists() => File.Exists(spotifyTokenPath);

                // saves token-file at class-path
                public void SaveToFile()
                {
                    var json = JsonConvert.SerializeObject(this);

                    Debug.WriteLine($"Saving token to path: {spotifyTokenPath}");

                    File.WriteAllText(spotifyTokenPath, json);
                }
            }

            public class YoutubeToken
            {
                public required string AccessToken { get; set; }
                public required string RefreshToken { get; set; }
                public DateTime IssuedUtc { get; set; }
                public long? ExpiresInSeconds { get; set; }
                public bool IsExpired => DateTime.UtcNow > IssuedUtc.AddSeconds((double)ExpiresInSeconds);

                // Gets a TOKENRESPONSE-USER type file
                public static YoutubeToken? LoadFromFile()
                {
                    if (TokenExists())
                    {
                        var json = File.ReadAllText(youtubeTokenPath);
                        return JsonConvert.DeserializeObject<YoutubeToken>(json);
                    }

                    return null;
                }

                public static bool TokenExists() => File.Exists(youtubeTokenPath);

                //public static bool TokenExpired()
                //{
                //    DateTime issuedTime = DateTime.Parse(token.IssuedUtc);
                //    return DateTime.UtcNow > issuedTime.AddSeconds(token.ExpiresInSeconds ?? 0);
                //}

                // Save token-file at class-path
                public void SaveToFile()
                {
                    Debug.WriteLine($"Saving token to path: {youtubeTokenPath}");

                    var json = JsonConvert.SerializeObject(this);

                    File.WriteAllText(youtubeTokenPath, json); // Error, denied?
                }

            }
        }

        // Gets Youtube Related Data
        public static class YoutubeAPI
        {

            public static string GetSecretKey() => jsonContent["YoutubeSecretKey"]!.ToString();

            public static string GetClientSecret() => jsonContent["YoutubeClientSecret"]!.ToString();

            // Works as GetClientSecret() with Initial Conversion to a GoogleClientSecrets type
            public static GoogleClientSecrets GetGoogleClientSecret() => jsonContent["YoutubeClientSecret"]!.ToObject<GoogleClientSecrets>()!;

            public static string GetClientId() => jsonContent["YoutubeClientId"]!.ToString();

            public static string GetApiKey() => jsonContent["YoutubeApiKey"]!.ToString();
        }

        // Gets Spotify Related Data
        public class SpotifyAPI
        {
            // Youtube Secret
            public static string GetSecretKey() => jsonContent["SpotifySecretKey"]!.ToString();

            public static string GetClientId() => jsonContent["SpotifyClientId"]!.ToString();
        }
    }
}
