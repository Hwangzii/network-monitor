using System;
using System.Globalization;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    public class DownPercentToDashOffsetConverter : IValueConverter
    {
        private const double Factor = 100.0; // cùng hệ với converter trên

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;

            if (value != null)
                percent = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

            percent = Math.Max(0, Math.Min(100, percent));

            // Offset = phần download đã chiếm, scale theo Factor
            return percent * Factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
