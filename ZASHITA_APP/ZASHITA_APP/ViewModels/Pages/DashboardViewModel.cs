using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZASHITA_APP.Dialogs;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.IO;

namespace ZASHITA_APP.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _totalFilesScanned;

        [ObservableProperty]
        private string _lastScan;

        [ObservableProperty]
        private int _malwareFound;

        [ObservableProperty]
        private string _systemStatus;

        [ObservableProperty]
        private double _protectionPercentage;

        [ObservableProperty]
        private int _counter = 0;

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }
        public void OnQuickScan()
        {
            string datasetPath = @"C:\Datasets\API_Functions.csv";
            System.Diagnostics.Debug.WriteLine($"Start quick scan");
            System.Diagnostics.Debug.WriteLine($"Dataset Path: {datasetPath}");

            if (!File.Exists(datasetPath))
            {
                System.Windows.MessageBox.Show($"Dataset file (API_Functions.csv) not found at: {datasetPath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            QuickScanDialog scanDialog = new QuickScanDialog(datasetPath);
            scanDialog.ShowDialog();
        }
    }
}