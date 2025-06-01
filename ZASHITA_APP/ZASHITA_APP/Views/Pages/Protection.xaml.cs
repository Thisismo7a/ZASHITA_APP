using ZASHITA_APP.ViewModels.Pages;
using Wpf.Ui.Controls;
using System.Windows;

namespace ZASHITA_APP.Views.Pages
{
    public partial class Protection : INavigableView<ProtectionViewModel>
    {
        public ProtectionViewModel ViewModel { get; }

        public Protection(ProtectionViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void Quick_Scan(object sender, RoutedEventArgs e)
        {
            ViewModel.OnQuickScan(); 
        }

        private void Full_Scan(object sender, RoutedEventArgs e)
        {
            ViewModel.OnFullScan();
        }

        private void Selective_Scan(Object sender, RoutedEventArgs e)
        {
            ViewModel.OnSelectiveScan();
        }




    }
}


