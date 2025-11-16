using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    public class PercentToStarGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;

            if (value is double d)
                percent = d;

            // Clamp 0–100
            percent = Math.Max(0, Math.Min(100, percent));

            // Nếu 0 thì vẫn trả về 0.0001* để không biến mất hẳn
            return new GridLength(Math.Max(percent, 0.0001), GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
