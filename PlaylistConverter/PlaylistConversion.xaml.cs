using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlaylistConverter
{
    /// <summary>
    /// Interaction logic for PlaylistConversion.xaml
    /// </summary>
    public partial class PlaylistConversion : Window
    {
        public static ObservableCollection<string> PlatformComboBoxOptions { get; set; } = [];
        public ObservableCollection<PlaylistInfo> Playlists { get; set; } = [];

        //static PlaylistInfo test = new("test", "desc", "me", 5);

        public PlaylistConversion()
        {
            InitializeComponent();
            ResetUrlTextBox();
            PopulateComboBoxes();
        }

        private void PopulateComboBoxes()
        {
            // Populate
            foreach (var platform in AppConfig.LoggedPlatforms)
            {
                PlatformComboBoxOptions.Add(platform);
            }

            PlatformComboBox.ItemsSource = PlatformComboBoxOptions;

            Debug.WriteLine($"Platforms Logged in at: {AppConfig.LoggedPlatforms.Count}");
        }

        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlatformComboBox.SelectedItem != null)
            {
                switch (PlatformComboBox.SelectedItem.ToString())
                {
                    case "Youtube":
                        Dispatcher.Invoke(() =>
                        {
                            SharedPlaylistTextBox.BorderBrush = Brushes.Red;
                        });
                        break;
                    case "Spotify":
                        Dispatcher.Invoke(() =>
                        {
                            SharedPlaylistTextBox.BorderBrush = Brushes.LimeGreen;
                        });
                        break;
                }
            }

            InitializePlaylists();
        }

        private void ResetUrlTextBox()
        {
            Dispatcher.Invoke(() =>
            {
                SharedPlaylistTextBlock.Visibility = Visibility.Collapsed;
                SharedPlaylistTextBox.Visibility = Visibility.Collapsed;
                SharedPlaylistTextBox.BorderBrush = Brushes.Black;
            });
        }

        private async void InitializePlaylists()
        {
            ObservableCollection<PlaylistInfo> playlistsToAdd = [];

            if (PlatformComboBox.SelectedItem is string selectedPlatform)
            {
                if (selectedPlatform == "Spotify")
                {
                    playlistsToAdd = await SpotifyAuthentication.GetPlaylistsAsync();
                }
                else if (selectedPlatform == "Youtube")
                {
                    playlistsToAdd = await YoutubeAuthentication.GetPlaylistsAsync();
                }
            }

            Dispatcher.Invoke(() =>
            {
                SharedPlaylistTextBlock.Visibility = PlatformComboBox.SelectedIndex != -1 ? Visibility.Visible : Visibility.Collapsed;
                SharedPlaylistTextBox.Visibility = PlatformComboBox.SelectedIndex != -1 ? Visibility.Visible : Visibility.Collapsed;

                Debug.WriteLine($"Clearing Playlists...");
                Playlists.Clear();

                Debug.WriteLine($"Adding Playlists...");
                foreach (PlaylistInfo playlist in playlistsToAdd)
                {
                    Playlists.Add(playlist);
                    Debug.WriteLine($"Playlist: {playlist.Name}");
                }
            });
        }
    }
}
