using System;
using System.Globalization;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    // values: [0]=valueKbps, [1]=maxKbps, [2]=height
    public class ValueToYConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return 0.0;

            double v = values[0] is double dv ? dv : 0;
            double max = values[1] is double dm ? dm : 1;
            double h = values[2] is double dh ? dh : 0;

            if (h <= 0) return 0.0;
            if (max <= 0) max = 1;

            double y = h - (v / max) * h;
            if (y < 0) y = 0;
            if (y > h) y = h;
            return y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
