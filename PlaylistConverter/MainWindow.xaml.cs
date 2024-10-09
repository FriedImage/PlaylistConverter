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
            //CheckSavedAuthentications();
        }

        //private List<string> CheckSavedAuthentications()
        //{
        //    List<string> savedAuthentications = [];
        //    foreach ()
        //    {
        //        savedAuthentications.Add();
        //    }

        //    return savedAuthentications;
        //}

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