using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Converters
{
    public class PercentToDashArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = System.Convert.ToDouble(value);
            double max = System.Convert.ToDouble(parameter);

            double dash = percent / 100.0 * max;
            return new DoubleCollection { dash, max };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
