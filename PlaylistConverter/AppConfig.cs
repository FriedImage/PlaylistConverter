using System.IO;
using Newtonsoft.Json.Linq;

namespace PlaylistConverter
{
    internal class AppConfig
    {
        //static readonly string jsonFileContent = File.ReadAllText("apikeys.json"); 

        static readonly string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        static string rootPath = Path.Combine(baseDirectory, @"..\..\..");
        readonly static string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\", "apikeys.json");
        private static readonly JObject jsonContent;

        static AppConfig()
        {
            string jsonFileContent = File.ReadAllText(jsonFilePath); // Reads content from json file
            jsonContent = JObject.Parse(jsonFileContent);
        }

        public static class Tokens
        {
            public static readonly string spotifyTokenPath = Path.Combine(rootPath, "Tokens", "spotify_token.json");
            public static readonly string youtubeTokenPath = Path.Combine(rootPath, "Tokens", "youtube_token.json");
        }

        // Gets Youtube Related Data
        public static class YoutubeAPI
        {

            public static string GetSecretKey()
            {
                return jsonContent["YoutubeSecretKey"]!.ToString();
            }

            public static string GetClientId()
            {
                return jsonContent["YoutubeClientId"]!.ToString();
            }

            public static string GetApiKey()
            {
                return jsonContent["YoutubeApiKey"]!.ToString();
            }
        }

        // Gets Spotify Related Data
        public class SpotifyAPI
        {
            // Youtube Secret
            public static string GetSecretKey()
            {
                return jsonContent["SpotifySecretKey"]!.ToString();
            }

            public static string GetClientId()
            {
                return jsonContent["SpotifyClientId"]!.ToString();
            }
        }
    }
}
