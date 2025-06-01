using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ZASHITA_APP.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "ZASHITA PRO";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Dashboard",
                Icon = new SymbolIcon { Symbol = SymbolRegular.WindowShield16 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "Protection",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ShieldTask28 },
                TargetPageType = typeof(Views.Pages.Protection)
            },
            new NavigationViewItem()
            {
                Content = "History",
                Icon = new SymbolIcon { Symbol = SymbolRegular.History20 },
                TargetPageType = typeof(Views.Pages.DataPage)
            },
            new NavigationViewItem()
            {
                Content = "Quarantine",
                Icon = new SymbolIcon { Symbol = SymbolRegular.LockClosed20 },
                TargetPageType = typeof(Views.Pages.Quarantin)
            },
           
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            //new NavigationViewItem()
           // {
           //     Content = "About",
           //     Icon = new SymbolIcon { Symbol = SymbolRegular.Info28 },
           //     TargetPageType = typeof(Views.Pages.SettingsPage)
           // },
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
