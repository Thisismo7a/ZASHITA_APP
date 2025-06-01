using System.Diagnostics;
using System.Windows.Controls; // Explicitly use System.Windows.Controls for Button
using Wpf.Ui.Controls; // For MessageBox
using ZASHITA_APP.ViewModels.Pages;

namespace ZASHITA_APP.Views.Pages
{
    public partial class SettingsPage : Page, INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private async void CheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Content = "Notifications enabled!",
                Title = "Settings",
                PrimaryButtonText = "OK"
            };
            await messageBox.ShowDialogAsync();
        }

        private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Debug output to confirm the handler is called
            Debug.WriteLine("Button_Click triggered!");

            if (sender is System.Windows.Controls.Button button)
            {
                Debug.WriteLine($"Button clicked: {button.Name}");

                try
                {
                    if (button.Name == "GitHubButton")
                    {
                        Debug.WriteLine("Opening GitHub link...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://github.com/thisismo7a",
                            UseShellExecute = true
                        });
                    }
                    else if (button.Name == "TelegramButton")
                    {
                        Debug.WriteLine("Opening Telegram link...");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://t.me/thisismo7a",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        // Fallback to show button name if no match
                        var messageBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Content = $"Unknown button: {button.Name}. Please check button names in XAML.",
                            Title = "Debug",
                            PrimaryButtonText = "OK"
                        };
                        await messageBox.ShowDialogAsync();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Content = $"Error opening link: {ex.Message}",
                        Title = "Error",
                        PrimaryButtonText = "OK"
                    };
                    await messageBox.ShowDialogAsync();
                }
            }
            else
            {
                // Debug if sender is not a button
                Debug.WriteLine("Sender is not a Button!");
                var messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Content = "Invalid sender type. Expected a Button.",
                    Title = "Debug",
                    PrimaryButtonText = "OK"
                };
                await messageBox.ShowDialogAsync();
            }
        }
    }
}