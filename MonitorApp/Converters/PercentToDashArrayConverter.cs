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
            double percent = 0;
            if (value != null)
                percent = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

            // Đọc chiều dài cung (ConverterParameter)
            double length = 0;
            if (parameter != null)
                length = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);

            // Clamp 0–100
            percent = Math.Max(0, Math.Min(100, percent));

            if (length <= 0)
                length = 1; // tránh chia 0

            // dash = phần tô, gap = phần trống
            double dash = length * (percent / 100.0);
            double gap = Math.Max(length - dash, 0.0001);

            // pattern chỉ gồm 1 đoạn tô + 1 đoạn trống
            return new DoubleCollection { dash, gap };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
