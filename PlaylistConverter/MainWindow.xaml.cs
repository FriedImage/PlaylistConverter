using Newtonsoft.Json.Linq;
using SpotifyAPI.Web.Http;
using SpotifyAPI.Web;
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
using Newtonsoft.Json;

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
        private YoutubeAuthentication? youtubeAuth; // not sure if needed
        private SpotifyAuthentication? spotifyAuth;

        // Constructor
        public MainWindow()
        {
            tokenFileWatcher.Changed += TokenFileWatcher_Changed;
            tokenFileWatcher.Renamed += TokenFileWatcher_Renamed;
            tokenFileWatcher.Deleted += TokenFileWatcher_Deleted;

            InitializeComponent();
            InitializeConfigurationManager();

            ResetText();
            UpdateClearTokensButton();
            CheckSavedAuthentications();
        }

        private void InitializeConfigurationManager()
        {
            if (!AppConfig.ConfigValid)
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
            var spotifyToken = SpotifyAuthentication.LoadFile();
            
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
            try
            {
                var service = await YoutubeAuthentication.GetYouTubeServiceAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Spotify authentication: {ex.Message}");
            }
            


            // Request - Response example (Playlist Titles)
            //var getPlaylistsRequest = service.Playlists.List("snippet");
            //getPlaylistsRequest.Mine = true;
            //var getPlaylistsResponse = getPlaylistsRequest.ExecuteAsync();
            //if (getPlaylistsResponse != null)
            //{
            //    foreach (var item in getPlaylistsResponse.Result.Items)
            //    {
            //        var playListName = item.Snippet.Title;

            //        Debug.WriteLine(playListName);
            //    }
            //}

            CheckSavedAuthentications();
        }

        private async void SpotifyLoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var spotify = await SpotifyAuthentication.GetSpotifyClientAsync();

                if (spotify != null)
                {
                    var me = await spotify.UserProfile.Current();
                    Debug.WriteLine($"Authenticated using saved tokens as: {me.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during Spotify authentication: {ex.Message}");
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