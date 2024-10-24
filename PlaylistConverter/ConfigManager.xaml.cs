using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// Interaction logic for ConfigManager.xaml
    /// </summary>
    public partial class ConfigManager : Window
    {
        private readonly string invalidStatus = "is Invalid!";
        private readonly string validStatus = "is Valid!";
        private static bool spotifyClientIdValid = false;
        private static bool spotifySecretValid = false;
        private static bool youtubeSecretsValid = false;
        private static string youtubeJsonFileName = string.Empty;
        private static string youtubeJsonFilePicked = string.Empty;
        private JObject config = [];

        public ConfigManager()
        {
            InitializeComponent();
            ValidateCheckBoxes();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/FriedImage/PlaylistConverter#",
                UseShellExecute = true
            });
        }

        private void HelpTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HelpButton_Click(sender, e);
        }

        private void ValidateSpotifyTextBox(TextBox textBox, Image statusImage, ref bool flag)
        {
            if (textBox.Text.Length == 32)
            {
                statusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
                statusImage.ToolTip = $"Field {validStatus}";
                flag = true;
            }
            else if (textBox.Text == string.Empty)
            {
                statusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/wait.png"));
                statusImage.ToolTip = $"Please insert into the field.";
                flag = false;
            }
            else
            {
                statusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/invalid.png"));
                statusImage.ToolTip = $"Field {invalidStatus}";
                flag = false;
            }
        }

        private void SpotifyClientIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSpotifyTextBox(SpotifyClientIdTextBox, SpotifyClientIdStatusImage, ref spotifyClientIdValid);
            ValidateSaveButton();
            UpdateSaveButtonTextBlock();
        }

        private void SpotifyClientSecretTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateSpotifyTextBox(SpotifyClientSecretTextBox, SpotifyClientSecretStatusImage, ref spotifySecretValid);
            ValidateSaveButton();
            UpdateSaveButtonTextBlock();
        }

        private void ValidateSaveButton()
        {
            // Ensure that at least two platforms are selected
            if (AppConfig.SelectedPlatforms.Count < 2)
            {
                AppConfig.ConfigValid = false;
                SaveButton.IsEnabled = false;
                return;
            }
            AppConfig.ConfigValid = true;

            // Turn false if selected platforms don't match criteria
            // Spotify
            if (AppConfig.SelectedPlatforms.Contains("Spotify"))
            {
                if (!spotifyClientIdValid || !spotifySecretValid)
                {
                    AppConfig.ConfigValid = false;
                }
            }

            // Youtube
            if (AppConfig.SelectedPlatforms.Contains("Youtube"))
            {
                if (!youtubeSecretsValid)
                {
                    AppConfig.ConfigValid = false;
                }
            }


            // If at least 2 platforms are selected and all selected platforms are valid, enable the button
            SaveButton.IsEnabled = AppConfig.ConfigValid;
        }

        private void UpdateSaveButtonTextBlock()
        {
            if (AppConfig.ConfigValid)
            {
                SaveButtonTextBlock.Text = "Ready to save!";
            }
            else if (AppConfig.SelectedPlatforms.Count < 2)
            {
                SaveButtonTextBlock.Text = $"Choose atleast {2 - AppConfig.SelectedPlatforms.Count} {(AppConfig.SelectedPlatforms.Count == 1 ? "service" : "services")} to save!";
            }
            else
            {
                SaveButtonTextBlock.Text = "Ensure the fields are correct first!";
            }
        }

        public static bool ValidateYoutubeClientSecrets(string jsonFilePath)
        {
            // Step 1: Read the JSON file
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine("File not found.");
                return false;
            }

            var jsonContent = File.ReadAllText(jsonFilePath);
            JObject clientSecrets;

            try
            {
                // Step 2: Parse JSON
                clientSecrets = JObject.Parse(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing JSON: " + ex.Message);
                return false;
            }

            // Step 3: Navigate to 'web' object
            var webSection = clientSecrets["web"];
            if (webSection == null)
            {
                Console.WriteLine("The 'web' section is missing.");
                return false;
            }

            // Step 4: Validate each field
            string? clientId = webSection["client_id"]?.ToString();
            if (string.IsNullOrEmpty(clientId) || !clientId.EndsWith(".apps.googleusercontent.com"))
            {
                Console.WriteLine("Invalid client_id.");
                return false;
            }

            string? projectId = webSection["project_id"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
            {
                Console.WriteLine("Invalid project_id.");
                return false;
            }

            string? authUri = webSection["auth_uri"]?.ToString();
            if (authUri != "https://accounts.google.com/o/oauth2/auth")
            {
                Console.WriteLine("Invalid auth_uri.");
                return false;
            }

            string? tokenUri = webSection["token_uri"]?.ToString();
            if (tokenUri != "https://oauth2.googleapis.com/token")
            {
                Console.WriteLine("Invalid token_uri.");
                return false;
            }

            string? certUrl = webSection["auth_provider_x509_cert_url"]?.ToString();
            if (certUrl != "https://www.googleapis.com/oauth2/v1/certs")
            {
                Console.WriteLine("Invalid auth_provider_x509_cert_url.");
                return false;
            }

            string? clientSecret = webSection["client_secret"]?.ToString();
            if (string.IsNullOrEmpty(clientSecret) || clientSecret.Length != 35)
            {
                Console.WriteLine("Invalid client_secret.");
                return false;
            }

            JArray redirectUris = (JArray)webSection["redirect_uris"];
            if (redirectUris == null || redirectUris.Count == 0)
            {
                Console.WriteLine("No redirect_uris found.");
                return false;
            }

            // If all checks pass
            Console.WriteLine("Validation successful.");
            return true;
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new()
            {
                DefaultExt = ".json",
                Filter = "JSON Files (*.json)|*.json"
            };

            var result = fileDialog.ShowDialog();

            if (result == true)
            {
                youtubeJsonFilePicked = fileDialog.FileName;
                youtubeSecretsValid = ValidateYoutubeClientSecrets(youtubeJsonFilePicked);
                //AppConfig.YoutubeJsonFilePath = fileDialog.FileName; // Sets the user's selected file as a path (not needed)
                
                if (!youtubeSecretsValid)
                {
                    youtubeJsonFilePicked = string.Empty;
                    ResetFilePicker();
                } 
                else
                {
                    youtubeJsonFileName = fileDialog.SafeFileName;
                    YoutubeClientSecretStatusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/valid.png"));
                    YoutubeClientSecretStatusImage.ToolTip = $"File {validStatus}";
                    FileTxtTextBlock.Text = youtubeJsonFileName;
                }
            }

            ValidateSaveButton();
            UpdateSaveButtonTextBlock();
        }

        private void ValidateCheckBoxes()
        {
            // Add/Remove selectedPlatforms
            if (SpotifyCheckBox.IsChecked == true)
            {
                if (!AppConfig.SelectedPlatforms.Contains("Spotify"))
                {
                    AppConfig.SelectedPlatforms.Add("Spotify");
                }
            }
            else
            {
                AppConfig.SelectedPlatforms.Remove("Spotify");
            }

            if (YoutubeCheckBox.IsChecked == true)
            {
                if (!AppConfig.SelectedPlatforms.Contains("Youtube"))
                {
                    AppConfig.SelectedPlatforms.Add("Youtube");
                }
            }
            else
            {
                AppConfig.SelectedPlatforms.Remove("Youtube");
            }

            SpotifyServiceGrid.Visibility = (bool)SpotifyCheckBox.IsChecked! ? Visibility.Visible : Visibility.Hidden;
            YoutubeServiceGrid.Visibility = (bool)YoutubeCheckBox.IsChecked! ? Visibility.Visible : Visibility.Hidden;

            Debug.WriteLine("Platforms Selected:");
            foreach (string platform in AppConfig.SelectedPlatforms)
            {
                Debug.WriteLine(platform);
            }

            ValidateSaveButton();
            UpdateSaveButtonTextBlock();
        }

        private void YoutubeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ValidateCheckBoxes();
        }

        private void YoutubeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ResetYoutubeFields();
            ValidateCheckBoxes();
            ValidateSaveButton();
        }

        private void SpotifyCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ResetSpotifyFields();
            ValidateCheckBoxes();
            ValidateSaveButton();
        }

        private void SpotifyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ValidateCheckBoxes();
        }

        // Field Resets
        private void ResetSpotifyFields()
        {
            spotifyClientIdValid = false;
            spotifySecretValid = false;
            SpotifyClientIdStatusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/wait.png"));
            SpotifyClientSecretStatusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/wait.png"));
            SpotifyClientSecretStatusImage.ToolTip = "Please insert into the field";
            SpotifyClientIdStatusImage.ToolTip = "Please insert into the field";
            SpotifyClientIdTextBox.Text = string.Empty;
            SpotifyClientSecretTextBox.Text = string.Empty;
        }

        private void ResetYoutubeFields()
        {
            youtubeSecretsValid = false;
            YoutubeClientSecretStatusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/wait.png"));
            YoutubeClientSecretStatusImage.ToolTip = "No file chosen";
            FileTxtTextBlock.Text = "No file chosen";
        }

        private void ResetFilePicker()
        {
            youtubeSecretsValid = false;
            YoutubeClientSecretStatusImage.Source = new BitmapImage(new Uri(AppConfig.rootPath + "img/wait.png"));
            YoutubeClientSecretStatusImage.ToolTip = "No file chosen";
            FileTxtTextBlock.Text = "No file chosen";
        }

        private void ResetCheckBoxes()
        {
            SpotifyCheckBox.IsChecked = false;
            YoutubeCheckBox.IsChecked = false;
        }

        private void ResetAll()
        {
            ResetSpotifyFields();
            ResetYoutubeFields();
            ResetFilePicker();
            ResetCheckBoxes();
            ValidateCheckBoxes();
        }

        private static void SaveConfigFile(JObject config)
        {
            File.WriteAllText(AppConfig.ConfigJsonFilePath, config.ToString());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean config JObject
            config = [];

            // Spotify
            if (AppConfig.SelectedPlatforms.Contains("Spotify"))
            {
                config["spotify"] = new JObject
                {
                    { "client_id", SpotifyClientIdTextBox.Text },
                    { "client_secret", SpotifyClientSecretTextBox.Text }
                };
            }

            // Youtube
            if (AppConfig.SelectedPlatforms.Contains("Youtube"))
            {
                AppConfig.YoutubeJsonFilePath = $"{AppConfig.configJsonDirectory}\\{youtubeJsonFileName}"; // Sets path to config's directory + selected file from user (name)

                config["youtube"] = new JObject
                {
                    { "config_path", AppConfig.YoutubeJsonFilePath }
                };

                File.Copy(youtubeJsonFilePicked, AppConfig.YoutubeJsonFilePath);
            }

            // -youtube
            SaveConfigFile(config);
            InitializeMainWindow();
        }

        // Load-up the MainWindow
        private void InitializeMainWindow()
        {
            Debug.WriteLine($"Is config valid? {AppConfig.ConfigValid}");

            if (AppConfig.ConfigValid)
            {
                string selectedPlatforms = string.Join(" ", AppConfig.SelectedPlatforms);

                MessageBox.Show($"Configuration was successful.\nSelected Platforms: {selectedPlatforms}", "Save Successful");

                MainWindow mainWindow = new();
                mainWindow.Show();

                // Close this window (ConfigManager)
                Close();
            }
            else
            {
                MessageBox.Show("Something went wrong, please retry the configuration process.", "Save Error");

                ResetAll();
            }
        }
    }
}
