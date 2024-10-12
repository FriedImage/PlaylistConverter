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

            if (spotifyTokenValid)
            {
                SpotifyLoginButton.IsEnabled = false;
            }
            
            if (youtubeTokenValid)
            {
                YoutubeLoginButton.IsEnabled = false;
            }
        }

        // Checks if Spotify authentication is valid from current session
        private static bool ValidateSpotifyToken()
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
                    return false;
                }
                else if (spotifyToken.RefreshToken == null)
                {
                    Debug.WriteLine("Validation Failed: Spotify Token is NULL");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Validation Failed: Spotify Token is NULL");
                return false;
            }

            // Token is valid
            return true;
        }

        // Checks if Youtube authentication is valid from current session
        private static bool ValidateYoutubeToken()
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
                    return false;
                }
                else if (youtubeToken.RefreshToken == null)
                {
                    Debug.WriteLine("Validation Failed: Youtube Token is NULL");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Validation Failed: Youtube Token is NULL");
                return false;
            }

            // Token is valid
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
        }
    }
}