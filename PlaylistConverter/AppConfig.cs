using System.Diagnostics;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlaylistConverter
{
    internal partial class AppConfig
    {
        //static readonly string jsonFileContent = File.ReadAllText("apikeys.json"); 
        //private static dynamic apiKeys;

        public static readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string rootPath = Path.Combine(baseDirectory, @"..\..\..\");
        public static readonly string jsonFilePath = Path.Combine(rootPath, "apikeys.json");
        public static readonly string spotifyTokenText = "Spotify Token";
        public static readonly string youtubeTokenText = "Youtube Token";
        public static readonly string authIsValidText = " is Valid!";
        public static readonly string authIsExpiredText = " is Expired!";
        public static readonly string authIsNull = " is Invalid!";
        private static readonly JObject jsonContent;

        static AppConfig()
        {
            string jsonFileContent = File.ReadAllText(jsonFilePath); // Reads content from json file
            jsonContent = JObject.Parse(jsonFileContent); // Set JObject data to apikey.json type data
        }

        // Contains all the Tokens used
        public static class Tokens
        {
            // folder containing the tokens
            public static string tokenFolderDirectory = Path.Combine(rootPath, "tokens");
            // type: JSON
            public static readonly string spotifyTokenPath = Path.Combine(GetTokenStorageFolderDirectory(), "spotify_token.json");
            // type: TOKENRESPONSE-USER
            public static readonly string youtubeTokenPath = Path.Combine(GetTokenStorageFolderDirectory(), "youtube_token.TokenResponse-user");

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

                    Debug.WriteLine($"Saving spotify token to path: {spotifyTokenPath}");

                    File.WriteAllText(spotifyTokenPath, json);
                }
            }

            // Token config for Youtube
            public class YoutubeToken
            {
                // Youtube token properties
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
                    Debug.WriteLine($"Saving youtube token to path: {youtubeTokenPath}");

                    var json = JsonConvert.SerializeObject(this);

                    File.WriteAllText(youtubeTokenPath, json);
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
            public static string GetSecretKey() => jsonContent["SpotifySecretKey"]!.ToString();

            public static string GetClientId() => jsonContent["SpotifyClientId"]!.ToString();
        }
    }
}
