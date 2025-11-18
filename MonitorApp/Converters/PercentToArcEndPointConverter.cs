using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MonitorApp.Converters
{
    public class PercentToArcEndPointConverter : IValueConverter
    {
        // ConverterParameter: "cx,cy,r"  (centerX, centerY, radius)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;

            if (value != null)
                percent = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

            percent = Math.Max(0, Math.Min(100, percent));
            double t = percent / 100.0;   // 0..1

            if (parameter == null)
                return new Point(180, 90); // fallback

            var parts = parameter.ToString().Split(',');
            if (parts.Length != 3)
                return new Point(180, 90);

            double cx = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double cy = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double r = double.Parse(parts[2], CultureInfo.InvariantCulture);

            // Nửa vòng TRÊN: đi từ 180° -> 360°
            // (WPF: trục Y hướng xuống, nên sin âm => điểm nằm phía trên tâm)
            double startAngle = Math.PI;             // 180°
            double angle = startAngle + Math.PI * t; // 180° + t*180° (đến 360°)

            double x = cx + r * Math.Cos(angle);
            double y = cy + r * Math.Sin(angle);

            return new Point(x, y);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
