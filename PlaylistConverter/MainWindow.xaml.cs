using System.Diagnostics;
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

namespace PlaylistConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CheckSavedAuthentications();
        }

        private void CheckSavedAuthentications()
        {
            bool spotifyTokenValid = ValidateSpotifyToken();
            bool youtubeTokenValid = ValidateYoutubeToken();

            // Spotify token validation
            if (spotifyTokenValid)
            {
                SpotifyAuthStatusValueLabel.Foreground = Brushes.LimeGreen;
                SpotifyLoginButton.IsEnabled = false;
                SpotifyAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
            }
            else
            {
                SpotifyAuthStatusValueLabel.Foreground = Brushes.OrangeRed;
                SpotifyLoginButton.IsEnabled = true;
                SpotifyAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/invalid.png"));
            }
            
            // Youtube token validation
            if (youtubeTokenValid)
            {
                YoutubeAuthStatusValueLabel.Foreground = Brushes.LimeGreen;
                YoutubeLoginButton.IsEnabled = false;
                YoutubeAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
            }
            else
            {
                YoutubeAuthStatusValueLabel.Foreground = Brushes.OrangeRed;
                YoutubeLoginButton.IsEnabled = true;
                YoutubeAuthStatusValidImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/invalid.png"));
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
                MessageBox.Show("Youtube Login Successful!");
            }
            else
            {
                MessageBox.Show("Youtube Login Failed");
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

                MessageBox.Show("Spotify Login Successful!");
            }
            else
            {
                MessageBox.Show("Spotify Login Failed");
            }

            CheckSavedAuthentications();
        }
    }
}