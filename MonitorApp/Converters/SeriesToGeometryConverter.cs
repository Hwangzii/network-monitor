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
    /// Smooth AREA geometry using Catmull-Rom -> Cubic Bezier.
    /// MultiBinding values:
    ///  [0] IList<double> series
    ///  [1] double width
    ///  [2] double height
    ///  [3] double maxY (optional; can be 0)
    /// </summary>
    public sealed class SeriesToGeometryConverter : IMultiValueConverter
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

            int n = series.Count;
            double stepX = width / (n - 1);

            // Build points (x, y)
            var pts = new List<Point>(n);
            for (int i = 0; i < n; i++)
            {
                double x = i * stepX;
                double y = height - (series[i] / maxY) * height;
                y = Math.Clamp(y, 0, height);
                pts.Add(new Point(x, y));
            }

            // Start from bottom-left -> up to first point
            var fig = new PathFigure
            {
                StartPoint = new Point(0, height),
                IsClosed = true,
                IsFilled = true
            };

            fig.Segments.Add(new LineSegment(pts[0], true));

            // Smooth curve: Catmull-Rom to Bezier
            for (int i = 0; i < n - 1; i++)
            {
                Point p0 = (i - 1) >= 0 ? pts[i - 1] : pts[i];
                Point p1 = pts[i];
                Point p2 = pts[i + 1];
                Point p3 = (i + 2) < n ? pts[i + 2] : pts[i + 1];

                // Catmull-Rom -> Bezier control points
                // c1 = p1 + (p2 - p0) / 6
                // c2 = p2 - (p3 - p1) / 6
                var c1 = new Point(
                    p1.X + (p2.X - p0.X) / 6.0,
                    p1.Y + (p2.Y - p0.Y) / 6.0
                );

                var c2 = new Point(
                    p2.X - (p3.X - p1.X) / 6.0,
                    p2.Y - (p3.Y - p1.Y) / 6.0
                );

                fig.Segments.Add(new BezierSegment(c1, c2, p2, true));
            }

            // Close down to bottom-right
            fig.Segments.Add(new LineSegment(new Point(width, height), true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            return geo;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
