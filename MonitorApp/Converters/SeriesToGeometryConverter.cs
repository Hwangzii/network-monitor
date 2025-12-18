using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Converters
{
    public class SeriesToGeometryConverter : IMultiValueConverter
    {
        private const double PointWidthPx = 30.0;  // ✅ Must match GraphViewModel constant

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return Geometry.Empty;

            var series = values[0] as IList<double>;
            if (series == null || series.Count == 0)
                return Geometry.Empty;

            if (values[1] is not double width || width <= 0)
                return Geometry.Empty;

            if (values[2] is not double height || height <= 0)
                return Geometry.Empty;

            double maxValue = 0;
            if (values[3] is double d)
                maxValue = d;

            // nếu DynamicMaxValue chưa set thì fallback theo series
            if (maxValue <= 0)
                maxValue = series.Max();

            if (maxValue <= 0)
                maxValue = 1; // tránh chia 0

            // ✅ Use consistent point width for infinite scroll
            double stepX = PointWidthPx;

            var figure = new PathFigure
            {
                // bắt đầu tại điểm đầu tiên (line chart)
                StartPoint = new Point(0, height - (series[0] / maxValue * height))
            };

            for (int i = 1; i < series.Count; i++)
            {
                double x = i * stepX;
                double y = height - (series[i] / maxValue * height);

                // clamp nếu có giá trị vượt max (do spike)
                if (y < 0) y = 0;
                if (y > height) y = height;

                figure.Segments.Add(new LineSegment(new Point(x, y), true));
            }

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
