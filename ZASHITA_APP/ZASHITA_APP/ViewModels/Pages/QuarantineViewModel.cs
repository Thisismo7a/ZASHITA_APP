using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;
using Wpf.Ui.Controls;
using System.Windows;

namespace ZASHITA_APP.ViewModels.Pages
{
    public partial class QuarantineViewModel : INotifyPropertyChanged, INavigationAware
    {
        private bool _isInitialized = false;
        private ObservableCollection<QuarantineItem> _quarantineItems = new ObservableCollection<QuarantineItem>();

        public ObservableCollection<QuarantineItem> QuarantineItems
        {
            get => _quarantineItems;
            set
            {
                _quarantineItems = value;
                OnPropertyChanged(nameof(QuarantineItems));
            }
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            LoadQuarantineData();
            _isInitialized = true;
        }

        private void LoadQuarantineData()
        {
            string databasePath = @"C:\Users\WORK\source\repos\Hamza19C\HAMZA_PFE\HAMZA_PFE\ZASHITA.sql";
            try
            {
                if (!File.Exists(databasePath))
                {
                    new Wpf.Ui.Controls.MessageBox
                    {
                        Title = "Database Error",
                        Content = $"Database file not found at: {databasePath}",
                        CloseButtonText = "OK"
                    }.ShowDialogAsync();
                    return;
                }

                using (var connection = new SqliteConnection($"Data Source={databasePath};"))
                {
                    connection.Open();
                    string query = "SELECT FilePath, ScanDate, Action FROM quarantin ORDER BY ScanDate DESC";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            QuarantineItems.Clear();
                            while (reader.Read())
                            {
                                string filePath = reader.GetString(0);
                                long scanDateUnix = reader.GetInt64(1);
                                int action = reader.GetInt32(2);
                                var scanDate = DateTimeOffset.FromUnixTimeSeconds(scanDateUnix).LocalDateTime;
                                string actionTaken = action switch
                                {
                                    0 => "Quarantined",
                                    1 => "Kept",
                                    2 => "Removed",
                                    _ => "Unknown"
                                };
                                QuarantineItems.Add(new QuarantineItem { FilePath = filePath, ScanDate = scanDate, ActionTaken = actionTaken });
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Database Error",
                    Content = $"Failed to load quarantine data: {ex.Message}\nPath: {databasePath}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }

        public void UpdateSelectedAction(QuarantineItem item, string newAction)
        {
            if (item == null) return;

            try
            {
                item.ActionTaken = newAction;
                UpdateDatabaseAction(item.FilePath, GetActionCode(newAction));
                OnPropertyChanged(nameof(QuarantineItems));
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Update Error",
                    Content = $"Failed to update action: {ex.Message}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }

        public void UpdateAllAction(string newAction)
        {
            try
            {
                foreach (var item in QuarantineItems)
                {
                    item.ActionTaken = newAction;
                    UpdateDatabaseAction(item.FilePath, GetActionCode(newAction));
                }
                OnPropertyChanged(nameof(QuarantineItems));
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Update Error",
                    Content = $"Failed to update all actions: {ex.Message}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }

        private int GetActionCode(string action)
        {
            return action switch
            {
                "Removed" => 2,
                "File restored" => 1,
                "Kept" => 1,
                "Quarantined" => 0,
                _ => 0
            };
        }

        private void UpdateDatabaseAction(string filePath, int actionCode)
        {
            string databasePath = @"C:\Users\WORK\source\repos\Hamza19C\HAMZA_PFE\HAMZA_PFE\ZASHITA.sql";
            try
            {
                using (var connection = new SqliteConnection($"Data Source={databasePath};"))
                {
                    connection.Open();
                    string query = "UPDATE quarantin SET Action = @Action WHERE FilePath = @FilePath";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Action", actionCode);
                        command.Parameters.AddWithValue("@FilePath", filePath);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Database Error",
                    Content = $"Failed to update action: {ex.Message}\nPath: {databasePath}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class QuarantineItem
    {
        public string FilePath { get; set; }
        public DateTime ScanDate { get; set; }
        public string ActionTaken { get; set; }
    }
}