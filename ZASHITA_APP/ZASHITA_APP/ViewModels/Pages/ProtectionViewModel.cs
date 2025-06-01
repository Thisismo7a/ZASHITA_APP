using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZASHITA_APP.Dialogs;
using System.IO;

namespace ZASHITA_APP.ViewModels.Pages
{
    public partial class ProtectionViewModel : ObservableObject
    {
        [RelayCommand]
        public void OnQuickScan()
        {
            string datasetPath = @"C:\Datasets\API_Functions.csv"; // make a new folder in the C disk and name it dataset , then put ur 256sha then name it API_Functions.csv :) By mo7aaaaa
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

        [RelayCommand]
        public void OnFullScan()
        {
            string datasetPath = @"C:\Datasets\API_Functions.csv"; // make a new folder in the C disk and name it dataset , then put ur 256sha then name it API_Functions.csv :) By mo7aaaaa
            System.Diagnostics.Debug.WriteLine($"Start full scan");
            System.Diagnostics.Debug.WriteLine($"Dataset Path: {datasetPath}");

            if (!File.Exists(datasetPath))
            {
                System.Windows.MessageBox.Show($"Dataset file (API_Functions.csv) not found at: {datasetPath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            FullScanDialog scanDialog = new FullScanDialog(datasetPath);
            scanDialog.ShowDialog();
        }

        [RelayCommand]
        public void OnSelectiveScan()
        {
            string datasetPath = @"C:\Datasets\API_Functions.csv"; // make a new folder in the C disk and name it dataset , then put ur 256sha then name it API_Functions.csv :) By mo7aaaaa 
            System.Diagnostics.Debug.WriteLine($"Start selective scan");
            System.Diagnostics.Debug.WriteLine($"Dataset Path: {datasetPath}");

            if (!File.Exists(datasetPath))
            {
                System.Windows.MessageBox.Show($"Dataset file (API_Functions.csv) not found at: {datasetPath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            SelectiveScanDialog scanDialog = new SelectiveScanDialog(datasetPath);
            scanDialog.ShowDialog();
        }
    }
}