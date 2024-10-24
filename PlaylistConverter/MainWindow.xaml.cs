﻿using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static PlaylistConverter.AppConfig;

namespace PlaylistConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // FileSystemWatcher for Tokens
        private readonly FileSystemWatcher tokenFileWatcher = new(AppConfig.Tokens.tokenFolderDirectory)
        {
            Filter = "*.*", // ALL file types
            NotifyFilter = NotifyFilters.LastAccess
                           | NotifyFilters.LastWrite
                           | NotifyFilters.FileName
                           | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
            InternalBufferSize = 64 * 1024 // 64kb
        };

        // DirectoryInfo to represent the token folder
        private readonly DirectoryInfo tokenFolderDirectory = new(Tokens.GetTokenStorageFolderDirectory());

        // Constructor
        public MainWindow()
        {
            tokenFileWatcher.Changed += TokenFileWatcher_Changed;
            tokenFileWatcher.Renamed += TokenFileWatcher_Renamed;
            tokenFileWatcher.Deleted += TokenFileWatcher_Deleted;

            InitializeComponent();
            CheckExistingConfiguration();
            InitializeConfigurationManager();

            ResetText();
            UpdateClearTokensButton();
            CheckSavedAuthentications();
        }

        private static bool CheckExistingConfiguration()
        {
            //if (!File.Exists(ConfigJsonFilePath)) return false;
            //if (!File.Exists(YoutubeJsonFilePath)) return false;

            if (File.Exists(ConfigJsonFilePath))
            {
                var configJson = JObject.Parse(File.ReadAllText(ConfigJsonFilePath));

                if (configJson["spotify"] != null && IsValidSpotifyConfig(configJson["spotify"]!))
                {
                    SelectedPlatforms.Add("Spotify");
                }
            }
            if (File.Exists(YoutubeJsonFilePath))
            {
                var youtubeConfigJson = JObject.Parse(File.ReadAllText(YoutubeJsonFilePath));

                if (youtubeConfigJson["web"] != null && IsValidYouTubeConfig(youtubeConfigJson["web"]!))
                {
                    SelectedPlatforms.Add("YouTube");
                }
            }

            Debug.WriteLine($"Platforms added: {SelectedPlatforms.Count}");

            return SelectedPlatforms.Count >= 2;
        }

        private static bool IsValidSpotifyConfig(JToken spotifyConfig)
        {
            return spotifyConfig["client_id"]?.ToString().Length == 32 &&
                   spotifyConfig["client_secret"]?.ToString().Length == 35;
        }

        private static bool IsValidYouTubeConfig(JToken youtubeConfig)
        {
            return youtubeConfig["client_id"]?.ToString().EndsWith(".apps.googleusercontent.com") == true &&
                   youtubeConfig["client_secret"]?.ToString().Length == 35;
        }

        private void InitializeConfigurationManager()
        {
            if (!CheckExistingConfiguration())
            {
                MessageBox.Show("No Token Configuration file found.\n\nTo use this application you must create/include a Client ID and Client Secret for each Affected Platforms Chosen.", "Token Configuration not found");

                ConfigManager configManager = new();
                configManager.Show();

                // Close this window (MainWindow)
                Close();
            }
        }

        private void ResetText()
        {
            Dispatcher.Invoke(() =>
            {
                SpotifyAuthStatusValueLabel.Content = AppConfig.spotifyTokenText;
                YoutubeAuthStatusValueLabel.Content = AppConfig.youtubeTokenText;
            });
        }

        private void UpdateClearTokensButton()
        {
            int tokenFileCount = tokenFolderDirectory.GetFiles().Length;

            Dispatcher.Invoke(() =>
            {
                ClearTokensButton.IsEnabled = tokenFileCount > 0;
            });
        }

        private void TokenFileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"File {e.Name} from {e.FullPath}");
            UpdateClearTokensButton();
            CheckSavedAuthentications();
        }

        private void TokenFileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine($"File renamed from {e.OldName} to {e.Name}");
            UpdateClearTokensButton();
            CheckSavedAuthentications();
        }

        private void TokenFileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");
            UpdateClearTokensButton();
            CheckSavedAuthentications();
        }

        private void CheckSavedAuthentications()
        {
            bool spotifyTokenValid = Dispatcher.Invoke(() => ValidateSpotifyToken());
            bool youtubeTokenValid = Dispatcher.Invoke(() => ValidateYoutubeToken());

            // Spotify token validation
            if (spotifyTokenValid)
            {
                Dispatcher.Invoke(() =>
                {
                    SpotifyAuthStatusValueLabel.Foreground = Brushes.LimeGreen;
                    SpotifyLoginButton.IsEnabled = false;
                    SpotifyAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
                });
            }
            else
            {
                

                Dispatcher.Invoke(() =>
                {
                    SpotifyAuthStatusValueLabel.Foreground = Brushes.OrangeRed;
                    SpotifyLoginButton.IsEnabled = true;
                    SpotifyAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/invalid.png"));
                });
            }

            // Youtube token validation
            if (youtubeTokenValid)
            {
                Dispatcher.Invoke(() => 
                {
                    YoutubeAuthStatusValueLabel.Foreground = Brushes.LimeGreen;
                    YoutubeLoginButton.IsEnabled = false;
                    YoutubeAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
                });
            }
            else
            {
                Dispatcher.Invoke(() => 
                {
                    YoutubeAuthStatusValueLabel.Foreground = Brushes.OrangeRed;
                    YoutubeLoginButton.IsEnabled = true;
                    YoutubeAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/invalid.png"));
                });
            }
        }

        // Checks if Spotify authentication is valid from current session
        private bool ValidateSpotifyToken()
        {
            var spotifyToken = AppConfig.Tokens.SpotifyToken.LoadFromFile();

            // One-way condition
            //if (spotifyToken != null && !spotifyToken.IsExpired && !string.IsNullOrEmpty(spotifyToken.RefreshToken))
            //{
            //    return true;
            //}

            // Reset before adding extension
            ResetText();

            // Precautions (Lead to false)
            if (spotifyToken != null)
            {
                if (spotifyToken.IsExpired)
                {
                    Debug.WriteLine("Validation Failed: Spotify Token is Expired");
                    SpotifyAuthStatusValueLabel.Content += AppConfig.authIsExpiredText;
                    return false;
                }
                else if (spotifyToken.RefreshToken == null)
                {
                    Debug.WriteLine("Validation Failed: Spotify Token is NULL");
                    SpotifyAuthStatusValueLabel.Content += AppConfig.authIsNull;
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Validation Failed: Spotify Token is NULL");
                SpotifyAuthStatusValueLabel.Content += AppConfig.authIsNull;
                return false;
            }

            // Token is valid
            SpotifyAuthStatusValueLabel.Content += AppConfig.authIsValidText;
            return true;
        }

        // Checks if Youtube authentication is valid from current session
        private bool ValidateYoutubeToken()
        {
            var youtubeToken = AppConfig.Tokens.YoutubeToken.LoadFromFile();

            // One-way condition
            //if (youtubeToken != null && !youtubeToken.IsExpired && !string.IsNullOrEmpty(youtubeToken.RefreshToken))
            //{
            //    return true;
            //}

            // Precautions (Lead to false)
            if (youtubeToken != null)
            {
                if (youtubeToken.IsExpired)
                {
                    Debug.WriteLine("Validation Failed: Youtube Token is Expired");
                    YoutubeAuthStatusValueLabel.Content += AppConfig.authIsExpiredText;
                    return false;
                }
                else if (youtubeToken.RefreshToken == null)
                {
                    Debug.WriteLine("Validation Failed: Youtube Token is NULL");
                    YoutubeAuthStatusValueLabel.Content += AppConfig.authIsNull;
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Validation Failed: Youtube Token is NULL");
                YoutubeAuthStatusValueLabel.Content += AppConfig.authIsNull;
                return false;
            }

            // Token is valid
            YoutubeAuthStatusValueLabel.Content += AppConfig.authIsValidText;
            return true;
        }

        private async void YoutubeLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var youtubeService = await PlatformAuthentications.YoutubeAuthentication.AuthenticateAsync();

            if (youtubeService != null)
            {
                MessageBox.Show("Youtube Authentication Successful!", "Login");
            }
            else
            {
                MessageBox.Show("Youtube Authentication Failed", "Login");
            }

            CheckSavedAuthentications();
        }

        private async void SpotifyLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var spotifyClient = await PlatformAuthentications.SpotifyAuthentication.AuthenticateUserAsync();

            if (spotifyClient != null)
            {
                var playlists = await spotifyClient.Playlists.CurrentUsers();

                foreach (var playlist in playlists.Items)
                {
                    Debug.WriteLine($"Spotify Playlist: {playlist.Name}");
                }

                MessageBox.Show("Spotify Authentication Successful!", "Login");
            }
            else
            {
                MessageBox.Show("Spotify Authentication Failed", "Login");
            }

            CheckSavedAuthentications();
        }

        private void ClearTokensButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = tokenFolderDirectory.GetFiles();

                foreach (FileInfo file in files)
                {
                    file.Delete();
                    Debug.WriteLine($"Deleted token-file: {file.Name}");
                }

                // Disable button
                UpdateClearTokensButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing tokens: {ex.Message}");
            }
        }
    }
}