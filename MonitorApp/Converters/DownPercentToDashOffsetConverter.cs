using System;
using System.Globalization;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    public class DownPercentToDashOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = System.Convert.ToDouble(value);
            double max = System.Convert.ToDouble(parameter);

            return percent / 100.0 * max;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
