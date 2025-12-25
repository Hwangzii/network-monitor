using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MonitorApp.Converters
{
    /// <summary>
    /// Build AREA geometry (closed path) with smooth curve (quadratic Bezier)
    /// values:
    ///  [0] IList<double> series
    ///  [1] double width
    ///  [2] double height
    ///  [3] double maxY (optional; can be 0)
    /// </summary>
    public class SeriesToGeometryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3) return Geometry.Empty;
            if (values[0] is not IList<double> series || series.Count < 2) return Geometry.Empty;
            if (values[1] is not double width || width <= 0) return Geometry.Empty;
            if (values[2] is not double height || height <= 0) return Geometry.Empty;

            double maxY = 0;
            if (values.Length >= 4 && values[3] is double d) maxY = d;
            if (maxY <= 0) maxY = Math.Max(1, series.Max());

            double stepX = width / (series.Count - 1);

            // Convert to points
            var pts = new Point[series.Count];
            for (int i = 0; i < series.Count; i++)
            {
                double x = i * stepX;
                double y = height - (series[i] / maxY) * height;
                y = Math.Clamp(y, 0, height);
                pts[i] = new Point(x, y);
            }

            var fig = new PathFigure
            {
                StartPoint = new Point(0, height), // start at bottom-left
                IsClosed = true,
                IsFilled = true
            };

            // up to first point
            fig.Segments.Add(new LineSegment(pts[0], true));

            // smooth curve using quadratic Bezier segments
            for (int i = 1; i < pts.Length; i++)
            {
                var p0 = pts[i - 1];
                var p1 = pts[i];

                // control point halfway on X, keep target Y for smoother curve
                var cx = (p0.X + p1.X) / 2.0;
                var c1 = new Point(cx, p0.Y);
                var c2 = new Point(cx, p1.Y);

                // 2 quadratic segments per step -> smoother
                fig.Segments.Add(new QuadraticBezierSegment(c1, new Point(cx, (p0.Y + p1.Y) / 2.0), true));
                fig.Segments.Add(new QuadraticBezierSegment(c2, p1, true));
            }

            // down to bottom-right then close
            fig.Segments.Add(new LineSegment(new Point(width, height), true));

            return new PathGeometry(new[] { fig });
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
