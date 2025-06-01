using ZASHITA_APP.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Threading.Tasks;

namespace ZASHITA_APP.Views.Pages
{
    public partial class Quarantin : Page
    {
        private readonly QuarantineViewModel _viewModel;

        public Quarantin()
        {
            _viewModel = new QuarantineViewModel();
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void QuarantineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuarantineListBox.SelectedItem is QuarantineItem item && item.ActionTaken == "Removed")
            {
                QuarantineListBox.SelectedItem = null;
            }
        }

        private async void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu)
            {
                if (contextMenu.Tag is QuarantineItem item)
                {
                    if (item.ActionTaken != "Quarantined")
                    {
                        return;
                    }

                    var confirmBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Confirm Removal",
                        Content = "Are you sure you want to remove this file?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "Cancel"
                    };

                    var result = await confirmBox.ShowDialogAsync();
                    if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                    {
                        _viewModel.UpdateSelectedAction(item, "Removed");
                        QuarantineListBox.SelectedItem = null;

                        var removingBox = new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "Removing File",
                            Content = "File is removing, wait a while...",
                            CloseButtonText = "OK"
                        };
                        var removingTask = removingBox.ShowDialogAsync();

                        await Task.Run(() =>
                        {
                            try
                            {
                                string quarantinePath = item.FilePath;
                                if (File.Exists(quarantinePath))
                                {
                                    File.Delete(quarantinePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                                {
                                    await new Wpf.Ui.Controls.MessageBox
                                    {
                                        Title = "Error",
                                        Content = $"Failed to delete file: {ex.Message}",
                                        CloseButtonText = "OK"
                                    }.ShowDialogAsync();
                                });
                            }
                        });
                    }
                }
            }
        }

        private async void KeepMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu)
            {
                if (contextMenu.Tag is QuarantineItem item)
                {
                    if (item.ActionTaken == "Removed")
                    {
                        new Wpf.Ui.Controls.MessageBox
                        {
                            Title = "Restore Error",
                            Content = "This file has been removed and cannot be restored.",
                            CloseButtonText = "OK"
                        }.ShowDialogAsync();
                        return;
                    }

                    var messageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Confirm Restoration",
                        Content = "Are you sure you want to restore this file?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "Cancel"
                    };

                    var result = await messageBox.ShowDialogAsync();
                    if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                    {
                        _viewModel.UpdateSelectedAction(item, "File restored");
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                {
                    return element;
                }
                var result = FindVisualChild<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}