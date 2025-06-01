using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Windows.Controls;

namespace ZASHITA_APP.Views.Pages
{
    public partial class DataPage : Page, INotifyPropertyChanged
    {
        private ObservableCollection<HistoryItem> _historyItems = new ObservableCollection<HistoryItem>();
        public ObservableCollection<HistoryItem> HistoryItems
        {
            get => _historyItems;
            set
            {
                _historyItems = value;
                OnPropertyChanged(nameof(HistoryItems));
            }
        }

        public DataPage()
        {
            DataContext = this;
            InitializeComponent();
            Loaded += DataPage_Loaded;
        }

        private void DataPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadHistoryData();
        }

        private void LoadHistoryData()
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

                    string query = "SELECT NumOfMalwares, NumOfFilesScaned, LastScan, StateOfSys FROM MainScan ORDER BY LastScan DESC";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            HistoryItems.Clear();
                            while (reader.Read())
                            {
                                int numOfMalwares = reader.GetInt32(0);
                                int numOfFilesScanned = reader.GetInt32(1);
                                long lastScanUnix = reader.GetInt64(2);
                                int stateOfSys = reader.GetInt32(3);

                                var date = DateTimeOffset.FromUnixTimeSeconds(lastScanUnix).LocalDateTime;
                                HistoryItems.Add(new HistoryItem
                                {
                                    Date = date,
                                    FilesScanned = numOfFilesScanned,
                                    MalwaresDetected = numOfMalwares,
                                    SystemStatus = stateOfSys == 0 ? "Secure" : "Not Secure",
                                    ScanType = "Moved to Quarantine" //here we allways use the ml model scan cuz the hash allways sais not a malware hhhhhh 
                                });// i changed it again .... i will do the quarantine rn (mo7a :D )
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
                    Content = $"Failed to load history data: {ex.Message}\nPath: {databasePath}",
                    CloseButtonText = "OK"
                }.ShowDialogAsync();
            }
        }

        private void HistoryDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HistoryItem
    {
        public DateTime Date { get; set; }
        public int FilesScanned { get; set; }
        public int MalwaresDetected { get; set; }
        public string SystemStatus { get; set; }
        public string ScanType { get; set; }
    }
}