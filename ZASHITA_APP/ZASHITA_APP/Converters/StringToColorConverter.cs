using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ZASHITA_APP.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Contains("secure", StringComparison.OrdinalIgnoreCase)
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
            }
            return System.Windows.Media.Brushes.Red; // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}