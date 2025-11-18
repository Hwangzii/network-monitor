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

            if (value != null)
                percent = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

            // Clamp 0–100
            percent = Math.Max(0, Math.Min(100, percent));

            // Nếu 0 thì vẫn để rất nhỏ để không nổ layout
            double star = Math.Max(percent, 0.0001);

            return new GridLength(star, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
