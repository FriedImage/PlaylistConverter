using System.Diagnostics;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace PlaylistConverter
{
    internal partial class AppConfig
    {
        //static readonly string jsonFileContent = File.ReadAllText("apikeys.json"); 
        //private static dynamic apiKeys;

        public static readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string rootPath = Path.Combine(baseDirectory, @"..\..\..\");
        public static readonly string jsonFilePath = Path.Combine(rootPath, "apikeys.json");
        public static readonly string configJsonDirectory = Path.Combine(rootPath, "settings");
        public static readonly string spotifyTokenText = "Spotify Token";
        public static readonly string youtubeTokenText = "Youtube Token";
        public static readonly string authIsValidText = " is Valid!";
        public static readonly string authIsExpiredText = " is Expired!";
        public static readonly string authIsNull = " is Invalid!";
        private static readonly JObject jsonContent;
        public static List<string> SelectedPlatforms { get; set; } = [];
        public static string ConfigJsonFilePath { get; set; } = Path.Combine(configJsonDirectory, "config.json");
        public static string YoutubeJsonFilePath { get; set; }
        public static bool ConfigValid { get; set; }
        public static string SpotifyClientId { get; set; }
        public static string SpotifyClientSecret { get; set; }

        // folder containing the tokens
        public static string tokenFolderDirectory = Path.Combine(rootPath, "tokens");
        public static readonly string spotifyTokenPath = Path.Combine(tokenFolderDirectory, "spotify_token.json");
        public static readonly string youtubeTokenPath = Path.Combine(tokenFolderDirectory, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");

        //private static readonly EmbedIOAuthServer _spotifyserver = new(new Uri("http://localhost:5543/callback"), 5543);

        static AppConfig()
        {
            string jsonFileContent = File.ReadAllText(jsonFilePath); // Reads content from json file
            jsonContent = JObject.Parse(jsonFileContent); // Set JObject data to apikey.json type data
            YoutubeJsonFilePath = GetYoutubeClientSecretsFile();
            ConfigValid = CheckExistingConfiguration();
            SpotifyClientId = GetSpotifyClientIdFromConfig(ConfigJsonFilePath);
            SpotifyClientSecret = GetSpotifyClientSecretFromConfig(ConfigJsonFilePath);
        }

        private static bool CheckExistingConfiguration()
        {
            if (File.Exists(ConfigJsonFilePath))
            {
                var configJson = JObject.Parse(File.ReadAllText(ConfigJsonFilePath));

                if (configJson["spotify"] != null && ValidateSpotifyConfig())
                {
                    SelectedPlatforms.Add("Spotify");
                }
            }
            if (File.Exists(YoutubeJsonFilePath))
            {
                var youtubeConfigJson = JObject.Parse(File.ReadAllText(YoutubeJsonFilePath));

                if (youtubeConfigJson["web"] != null && ValidateYouTubeConfig())
                {
                    SelectedPlatforms.Add("YouTube");
                }
            }

            Debug.WriteLine($"Platforms added: {SelectedPlatforms.Count}");

            return SelectedPlatforms.Count >= 2;
        }

        private static bool ValidateSpotifyConfig()
        {
            var configJson = JObject.Parse(File.ReadAllText(ConfigJsonFilePath));
            var spotifyConfig = configJson["spotify"];
            return spotifyConfig != null &&
                   spotifyConfig["client_id"]?.ToString().Length == 32 &&
                   spotifyConfig["client_secret"]?.ToString().Length == 32;
        }

        private static bool ValidateYouTubeConfig()
        {
            var youtubeConfigJson = JObject.Parse(File.ReadAllText(YoutubeJsonFilePath));
            var youtubeConfig = youtubeConfigJson["web"];
            return youtubeConfig != null &&
                   youtubeConfig["client_id"]?.ToString().EndsWith(".apps.googleusercontent.com") == true &&
                   youtubeConfig["client_secret"]?.ToString().Length == 35;
        }

        // Set the file path of Youtube's client_secret if it already exists
        private static string GetYoutubeClientSecretsFile()
        {
            if (File.Exists(ConfigJsonFilePath))
            {
                // Load config.json and parse it
                var configData = JObject.Parse(File.ReadAllText(ConfigJsonFilePath));
                Debug.WriteLine($"Config found at: {ConfigJsonFilePath}");

                // Check if the YouTube config path exists
                var youtubeConfigPath = configData["youtube"]?["config_path"]?.ToString();

                if (!string.IsNullOrEmpty(youtubeConfigPath) && youtubeConfigPath.EndsWith(".json"))
                {
                    Debug.WriteLine($"Youtube Config (secrets) found at: {youtubeConfigPath}");
                    return youtubeConfigPath;
                }
            }

            // File not found
            return string.Empty;
        }

        private static string GetSpotifyClientIdFromConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));
                var clientId = configFile["spotify"]?["client_id"]?.ToString();

                if (!string.IsNullOrEmpty(clientId) && ValidateSpotifyConfig())
                {
                    return clientId;
                }
            }

            return string.Empty;
        }

        private static string GetSpotifyClientSecretFromConfig(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                JObject configFile = JObject.Parse(File.ReadAllText(configFilePath));
                var clientSecret = configFile["spotify"]?["client_secret"]?.ToString();

                if (!string.IsNullOrEmpty(clientSecret) && ValidateSpotifyConfig())
                {
                    return clientSecret;
                }
            }

            return string.Empty;
        }

        // Contains all the Tokens used
        public static class Tokens
        {
            // folder containing the tokens
            public static string tokenFolderDirectory = Path.Combine(rootPath, "tokens");
            // type: JSON
            public static readonly string spotifyTokenPath = Path.Combine(GetTokenStorageFolderDirectory(), "spotify_token.json");
            // type: TOKENRESPONSE-USER
            public static readonly string youtubeTokenPath = Path.Combine(GetTokenStorageFolderDirectory(), "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");

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
