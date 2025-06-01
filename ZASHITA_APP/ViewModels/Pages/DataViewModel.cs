using System.Windows.Media;
using ZASHITA_APP.Models;
using Wpf.Ui.Controls;

namespace ZASHITA_APP.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private IEnumerable<DataColor> _colors;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            var random = new Random();
            var colorCollection = new List<DataColor>();

            for (int i = 0; i < 8192; i++)
            {
                colorCollection.Add(new DataColor
                {
                    Color = new SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(  
                            200,  
                            (byte)random.Next(0, 256),  
                            (byte)random.Next(0, 256),
                            (byte)random.Next(0, 256)
                        )
                    )
                });
            }


            Colors = colorCollection;

            _isInitialized = true;
        }
    }
}
