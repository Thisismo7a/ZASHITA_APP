using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;
using ZASHITA_APP.ViewModels.Pages;

namespace ZASHITA_APP.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void LoadDashboardData()
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
                    SetDefaultValues();
                    return;
                }

                using (var connection = new SqliteConnection($"Data Source={databasePath};"))
                {
                    connection.Open();

                    string checkTableQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='MainScan';";
                    using (var checkCommand = new SqliteCommand(checkTableQuery, connection))
                    {
                        if (checkCommand.ExecuteScalar() == null)
                        {
                            new Wpf.Ui.Controls.MessageBox
                            {
                                Title = "Database Error",
                                Content = "MainScan table does not exist.",
                                CloseButtonText = "OK"
                            }.ShowDialogAsync();
                            SetDefaultValues();
                            return;
                        }
                    }

                    
                    string sumQuery = "SELECT SUM(NumOfFilesScaned), SUM(NumOfMalwares) FROM MainScan";
                    int totalFilesScannedSum = 0;
                    int totalMalwaresSum = 0;
                    using (var sumCommand = new SqliteCommand(sumQuery, connection))
                    {
                        using (var sumReader = sumCommand.ExecuteReader())
                        {
                            if (sumReader.Read())
                            {
                                totalFilesScannedSum = sumReader.IsDBNull(0) ? 0 : sumReader.GetInt32(0);
                                totalMalwaresSum = sumReader.IsDBNull(1) ? 0 : sumReader.GetInt32(1);
                            }
                        }
                    }

                    
                    string lastRowQuery = "SELECT NumOfMalwares, NumOfFilesScaned, LastScan, StateOfSys FROM MainScan ORDER BY LastScan DESC LIMIT 1";
                    string message;
                    using (var command = new SqliteCommand(lastRowQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int numOfMalwares = reader.GetInt32(0);
                                int numOfFilesScaned = reader.GetInt32(1);
                                long lastScanUnix = reader.GetInt64(2);
                                int stateOfSys = reader.GetInt32(3);

                                var lastScanDate = DateTimeOffset.FromUnixTimeSeconds(lastScanUnix).UtcDateTime;
                                message = $"Last MainScan row:\nNumOfMalwares: {numOfMalwares}\nNumOfFilesScaned: {numOfFilesScaned}\nLastScan: {lastScanDate:yyyy-MM-dd HH:mm:ss}\nStateOfSys: {stateOfSys}\n\nTotals:\nTotal Files Scanned: {totalFilesScannedSum}\nTotal Malwares Found: {totalMalwaresSum}";

                                
                                tootalfilesscaned.Text = totalFilesScannedSum.ToString();
                                lastscanhaha.Text = GetRelativeTime(lastScanDate);
                                totalmalwaresdamn.Text = totalMalwaresSum.ToString();

                                
                                ViewModel.MalwareFound = totalMalwaresSum;
                                ViewModel.TotalFilesScanned = totalFilesScannedSum;
                                ViewModel.LastScan = GetRelativeTime(lastScanDate);
                                ViewModel.SystemStatus = stateOfSys == 0 ? "Your system is secure" : "Your system is not secure";
                                ViewModel.ProtectionPercentage = ViewModel.TotalFilesScanned > 0
                                    ? Math.Round((double)(ViewModel.TotalFilesScanned - ViewModel.MalwareFound) / ViewModel.TotalFilesScanned * 100, 2)
                                    : 0;
                            }
                            else
                            {
                                message = "No data found in MainScan table.\nPlease run a scan.";
                                SetDefaultValues();
                            }

                            new Wpf.Ui.Controls.MessageBox // u can remove this from the beginin of the app :) 
                            {
                                Title = "Database check ...",
                                Content = message,
                                CloseButtonText = "OK"
                            }.ShowDialogAsync();
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
                    Content = $"Failed to load data: {ex.Message}\nPath: {databasePath}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
                SetDefaultValues();
            }
        }

        private void SetDefaultValues()
        {
            ViewModel.MalwareFound = 0;
            ViewModel.TotalFilesScanned = 0;
            ViewModel.LastScan = "No scans yet";
            ViewModel.SystemStatus = "No scan data available";
            ViewModel.ProtectionPercentage = 0;
            tootalfilesscaned.Text = "0";
            lastscanhaha.Text = "No scans yet";
            totalmalwaresdamn.Text = "0";
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var timeSpan = now - dateTime;
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalHours < 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalDays < 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ViewModel.OnQuickScan();
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}